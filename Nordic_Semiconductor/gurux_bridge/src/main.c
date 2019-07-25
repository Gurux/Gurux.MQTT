//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

#include <zephyr.h>
#include <stdio.h>
#include <uart.h>
#include <string.h>
#include <dk_buttons_and_leds.h>

#include <net/mqtt.h>
#include <net/socket.h>
#include <lte_lc.h>
#include "gxmessage.h"
#include "gxparser.h"

//Received message.
gxMessage msg;
//Device name. Each device must have own unique name.
char DEVICE_NAME[100];

static struct device *uart_dev;
static bool frame_rdy;
static u8_t frame_buf[CONFIG_MQTT_MESSAGE_BUFFER_SIZE];
#define APP_CONNECT_TRIES	10
#define APP_SLEEP_MSECS		500

/* Buffers for MQTT client. */
static u8_t rx_buffer[CONFIG_MQTT_MESSAGE_BUFFER_SIZE];
static u8_t tx_buffer[CONFIG_MQTT_MESSAGE_BUFFER_SIZE];
static u8_t payload_buf[CONFIG_MQTT_PAYLOAD_BUFFER_SIZE];
static u8_t reply_buf[CONFIG_MQTT_PAYLOAD_BUFFER_SIZE];

/* The mqtt client struct */
static struct mqtt_client client;

/* MQTT Broker details. */
static struct sockaddr_storage broker;

/* Connected flag */
static bool connected;

/* File descriptor */
static struct pollfd fds;

#if defined(CONFIG_BSD_LIBRARY)

/**@brief Recoverable BSD library error. */
void bsd_recoverable_error_handler(uint32_t err)
{
	printk("bsdlib recoverable error: %u\n", err);
}

/**@brief Irrecoverable BSD library error. */
void bsd_irrecoverable_error_handler(uint32_t err)
{
	printk("bsdlib irrecoverable error: %u\n", err);

	__ASSERT_NO_MSG(false);
}

#endif /* defined(CONFIG_BSD_LIBRARY) */

/**@brief Function to print strings without null-termination
 */
static void data_print(u8_t *prefix, u8_t *data, size_t len)
{
	char buf[len + 1];

	memcpy(buf, data, len);
	buf[len] = 0;
	printk("%s%s\n", prefix, buf);
}

/**@brief Function to publish data on the configured topic
 */
static int data_publish(struct mqtt_client *c, enum mqtt_qos qos,
	u8_t *data, size_t len, const char *topic)
{
	struct mqtt_publish_param param;
	param.message.topic.qos = qos;
	param.message.topic.topic.utf8 = topic;
	param.message.topic.topic.size = strlen(topic);
	param.message.payload.data = data;
	param.message.payload.len = len;
	param.message_id = sys_rand32_get();
	param.dup_flag = 0;
	param.retain_flag = 0;
	return mqtt_publish(c, &param);
}

/**@brief Function to subscribe to the configured topic
 */
static int subscribe(void)
{
	struct mqtt_topic subscribe_topic = {
		.topic = {
			.utf8 = DEVICE_NAME,
			.size = strlen(DEVICE_NAME)
		},
		.qos = MQTT_QOS_0_AT_MOST_ONCE
	};

	const struct mqtt_subscription_list subscription_list = {
		.list = &subscribe_topic,
		.list_count = 1,
		.message_id = 1234
	};
	return mqtt_subscribe(&client, &subscription_list);
}

/**@brief Function to read the published payload.
 */
static int publish_get_payload(struct mqtt_client *c, size_t length)
{
	u8_t *buf = payload_buf;
	u8_t *end = buf + length;

	if (length > sizeof(payload_buf)) {
		return -EMSGSIZE;
	}

	while (buf < end) {
		int ret = mqtt_read_publish_payload(c, buf, end - buf);

		if (ret < 0) {
			int err;

			if (ret != -EAGAIN) {
				return ret;
			}

			printk("mqtt_read_publish_payload: EAGAIN\n");

			err = poll(&fds, 1, K_SECONDS(CONFIG_MQTT_KEEPALIVE));
			if (err > 0 && (fds.revents & POLLIN) == POLLIN) {
				continue;
			} else {
				return -EIO;
			}
		}

		if (ret == 0) {
			return -EIO;
		}

		buf += ret;
	}

	return 0;
}

unsigned char gx_getValue(char c)
{
    unsigned char value;
    if (c > '9')
    {
        if (c > 'Z')
        {
            value = (c - 'a' + 10);
        }
        else
        {
            value = (c - 'A' + 10);
        }
    }
    else
    {
        value = (c - '0');
    }
    return value;
}

int gx_hexToBytes(
    const char* str,
    unsigned char* buffer,
    unsigned short* count)
{
    *count = 0;
    if (str == NULL)
    {
        return 0;
    }
    int len = (int)strlen(str);
    if (len == 0)
    {
        return 0;
    }
    int lastValue = -1;
    for (int pos = 0; pos != len; ++pos)
    {
        if (*str >= '0' && *str < 'g')
        {
            if (lastValue == -1)
            {
                lastValue = gx_getValue(*str);
            }
            else if (lastValue != -1)
            {
                buffer[*count] = (unsigned char)(lastValue << 4 | gx_getValue(*str));
                lastValue = -1;
                ++*count;
            }
        }
        else if (lastValue != -1)
        {
            buffer[*count] = gx_getValue(*str);
            lastValue = -1;
            ++*count;
        }
        ++str;
    }
    return 0;
}

void hlp_bytesToHex(const unsigned char* pBytes, int count, char* hexChars)
{
    const char hexArray[] = { '0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F' };
    unsigned char tmp;
    int pos;
    if (count != 0)
    {
      for (pos = 0; pos != count; ++pos)
      {
          tmp = pBytes[pos] & 0xFF;
          hexChars[pos * 3] = hexArray[tmp >> 4];
          hexChars[pos * 3 + 1] = hexArray[tmp & 0x0F];
          hexChars[pos * 3 + 2] = ' ';
      }
      hexChars[(3 * count) - 1] = '\0';
    }
    else
    {
        hexChars[0] = '\0';
    }
}

/**@brief MQTT client event handler
 */
void mqtt_evt_handler(struct mqtt_client *const c,
		      const struct mqtt_evt *evt)
{
	int err;

	switch (evt->type) {
	case MQTT_EVT_CONNACK:
		if (evt->result != 0) {
                    break;
		}
		connected = true;
		subscribe();
		break;

	case MQTT_EVT_DISCONNECT:
		connected = false;
		break;

	case MQTT_EVT_PUBLISH: {
		const struct mqtt_publish_param *p = &evt->param.publish;
		err = publish_get_payload(c, p->message.payload.len);
		if (err >= 0) {
                        if (loadMessage(payload_buf, &msg) == 0)
                        {
                          unsigned short len = CONFIG_MQTT_PAYLOAD_BUFFER_SIZE;
                          if (msg.type == GX_MESSAGE_TYPE_OPEN ||
                              msg.type == GX_MESSAGE_TYPE_CLOSE)
                          {
                            dk_set_led_on(0x03);
                            saveMessage(&msg, reply_buf, &len);
                            data_publish(&client, MQTT_QOS_0_AT_MOST_ONCE, reply_buf, len, msg.sender);
                          }else if (msg.type == GX_MESSAGE_TYPE_SEND){
                            gx_hexToBytes(msg.frame, frame_buf, &len);
                            //Send received message to the serial port.
                            for (size_t i = 0; i < len; i++) {
                              uart_poll_out(uart_dev, frame_buf[i]);
                            }
                          }
                        }
		} else {
                        dk_set_leds_state(0x00, DK_ALL_LEDS_MSK);
			err = mqtt_disconnect(c);
			if (err) {
                          printk("Could not disconnect: %d\n", err);
			}
		}
	} break;
	case MQTT_EVT_PUBACK:
		if (evt->result != 0) {
                      printk("MQTT PUBACK error %d\n", evt->result);
                      break;
		}
		break;
	case MQTT_EVT_SUBACK:
		if (evt->result != 0) {
                  printk("MQTT SUBACK error %d\n", evt->result);
                    break;
		}
		break;
	default:
		break;
	}
}

/**@brief Resolves the configured hostname and
 * initializes the MQTT broker structure
 */
static void broker_init(void)
{
	int err;
	struct addrinfo *result;
	struct addrinfo *addr;
	struct addrinfo hints = {
		.ai_family = AF_INET,
		.ai_socktype = SOCK_STREAM
	};

	err = getaddrinfo(CONFIG_MQTT_BROKER_HOSTNAME, NULL, &hints, &result);
	if (err) {
		printk("ERROR: getaddrinfo failed %d\n", err);

		return;
	}

	addr = result;
	err = -ENOENT;

	/* Look for address of the broker. */
	while (addr != NULL) {
		/* IPv4 Address. */
		if (addr->ai_addrlen == sizeof(struct sockaddr_in)) {
			struct sockaddr_in *broker4 =
				((struct sockaddr_in *)&broker);

			broker4->sin_addr.s_addr =
				((struct sockaddr_in *)addr->ai_addr)
				->sin_addr.s_addr;
			broker4->sin_family = AF_INET;
			broker4->sin_port = htons(CONFIG_MQTT_BROKER_PORT);
			printk("IPv4 Address found 0x%08x\n",
				broker4->sin_addr.s_addr);
			break;
		} else {
			printk("ai_addrlen = %u should be %u or %u\n",
				(unsigned int)addr->ai_addrlen,
				(unsigned int)sizeof(struct sockaddr_in),
				(unsigned int)sizeof(struct sockaddr_in6));
		}

		addr = addr->ai_next;
		break;
	}

	/* Free the address. */
	freeaddrinfo(result);
}

/**@brief Initialize the MQTT client structure
 */
static void client_init(struct mqtt_client *client)
{
	mqtt_client_init(client);

	broker_init();

	/* MQTT client configuration */
	client->broker = &broker;
	client->evt_cb = mqtt_evt_handler;
	client->client_id.utf8 = (u8_t *)DEVICE_NAME;
	client->client_id.size = strlen(DEVICE_NAME);
	client->password = NULL;
	client->user_name = NULL;
	client->protocol_version = MQTT_VERSION_3_1_1;

	/* MQTT buffers configuration */
	client->rx_buf = rx_buffer;
	client->rx_buf_size = sizeof(rx_buffer);
	client->tx_buf = tx_buffer;
	client->tx_buf_size = sizeof(tx_buffer);

	/* MQTT transport configuration */
	client->transport.type = MQTT_TRANSPORT_NON_SECURE;
}

/**@brief Initialize the file descriptor structure used by poll.
 */
static int fds_init(struct mqtt_client *c)
{
	if (c->transport.type == MQTT_TRANSPORT_NON_SECURE) {
		fds.fd = c->transport.tcp.sock;
	} else {
#if defined(CONFIG_MQTT_LIB_TLS)
		fds.fd = c->transport.tls.sock;
#else
		return -ENOTSUP;
#endif
	}

	fds.events = POLLIN;

	return 0;
}

/**@brief Configures modem to provide LTE link. Blocks until link is
 * successfully established.
 */
static void modem_configure(void)
{
#if defined(CONFIG_LTE_LINK_CONTROL)
	if (IS_ENABLED(CONFIG_LTE_AUTO_INIT_AND_CONNECT)) {
		/* Do nothing, modem is already turned on
		 * and connected.
		 */
                  dk_set_led_on(0x02);
	} else {
		int err;

		//printk("LTE Link Connecting ...\n");
                dk_set_led_on(0x00);
		err = lte_lc_init_and_connect();
                if (err == 0)
                {
                  dk_set_led_on(0x01);
                }
                else
                {
                  dk_set_led_off(0x00);
                }
	}
#endif
}

unsigned int replyIndex = 0;

static void sendData()
{
  if (frame_rdy)
  {
    uart_irq_rx_disable(uart_dev);
    frame_rdy = false;
    //Send reply.
    msg.frame[0] = '\0';
    msg.type = GX_MESSAGE_TYPE_RECEIVE;
    hlp_bytesToHex(reply_buf, replyIndex, msg.frame);
    unsigned short len = CONFIG_MQTT_PAYLOAD_BUFFER_SIZE;
    saveMessage(&msg, reply_buf, &len);
    int err = data_publish(&client, MQTT_QOS_0_AT_MOST_ONCE, reply_buf, len, msg.sender);
    if (err != 0){
        printk("data_publish failed %d\r", err);
    }
    msg.frame[0] = '\0';
    replyIndex = 0;
    uart_irq_rx_enable(uart_dev);
  }
}

static void isr(struct device *x)
{
  uart_irq_update(x);
  if (uart_irq_rx_ready(x)) {
    char ch;
    int ret = uart_fifo_read(x, &ch, 1);
    if (ret != 0)
    {
      reply_buf[replyIndex] = ch;
      ++replyIndex;
      if ((ch == 0x7E || ch == '\r' || ch == '\n') && replyIndex != 1){
        frame_rdy = true;
      }
    }
  }
}

static int gx_uart_init(char *uart_dev_name)
{
  int err;
  uart_dev = device_get_binding(uart_dev_name);
  if (uart_dev == NULL) {
    printf("Cannot bind %s\n", uart_dev_name);
    return EINVAL;
  }
  err = uart_err_check(uart_dev);
  if (err) {
    printf("UART check failed\n");
    return EINVAL;
  }
  
  struct uart_config cfg;
  uart_config_get(uart_dev, &cfg);
  cfg.baudrate = 9600;
//  cfg.parity = 0;
//  cfg.stop_bits = 8;
//  cfg.data_bits = 1;
//  cfg.flow_ctrl = 1;
  err = uart_configure(uart_dev, &cfg);
  uart_irq_callback_set(uart_dev, isr);
  uart_irq_rx_enable(uart_dev);
  return err;
}

static int try_to_connect(struct mqtt_client *c)
{
	int rc, i = 0;

	while (i++ < APP_CONNECT_TRIES && !connected) {
		client_init(c);
		rc = mqtt_connect(c);
		if (rc != 0) {
			printk("mqtt_connect %d\n", rc);
			k_sleep(APP_SLEEP_MSECS);
			continue;
		}
		rc = fds_init(c);
		if (rc != 0) {
			printk("ERROR: fds_init %d\n", rc);
			return -EINVAL;
		}

		if (poll(&fds, 1, K_SECONDS(CONFIG_MQTT_KEEPALIVE)) < 0) {
			printk("poll error: %d\n", errno);
		}
		mqtt_input(c);

		if (!connected) {
			mqtt_abort(c);
		}
	}

	if (connected) {
		return 0;
	}

	return -EINVAL;
}

#define MY_STACK_SIZE 5000
#define MY_PRIORITY 9

K_THREAD_STACK_DEFINE(my_stack_area, MY_STACK_SIZE);
struct k_thread my_thread_data;

K_THREAD_STACK_DEFINE(my_stack_area2, MY_STACK_SIZE);
struct k_thread my_thread_data2;


static void mqtt_connectThread(void *a, void *b, void *c)
{
  int err;
  while (1) {
      err = try_to_connect(&client);
      if (err != 0) 
      { 
        dk_set_leds_state(0x00, DK_ALL_LEDS_MSK);
        return; 
      }
      while (connected) { 
              err = poll(&fds, 1, K_SECONDS(CONFIG_MQTT_KEEPALIVE));
              if (err < 0) {
                      err = dk_set_leds_state(0x00, DK_ALL_LEDS_MSK);
                      dk_set_led_on(0x00);                      
                      break;
              }

              err = mqtt_live(&client);
              if (err != 0) {
                      err = dk_set_leds_state(0x00, DK_ALL_LEDS_MSK);
                                              dk_set_led_on(0x01);                                             
                 break;
              }

              if ((fds.revents & POLLIN) == POLLIN) {
                      err = mqtt_input(&client);
                      if (err != 0) {
                          err = dk_set_leds_state(0x00, DK_ALL_LEDS_MSK);    
                          dk_set_led_on(0x02);                          
                          break;
                      }
              }

              if ((fds.revents & POLLERR) == POLLERR) {
                      err = dk_set_leds_state(0x00, DK_ALL_LEDS_MSK);			
                      dk_set_led_on(0x03);                      
                      break;
              }

              if ((fds.revents & POLLNVAL) == POLLNVAL) {
                      err = dk_set_leds_state(0x00, DK_ALL_LEDS_MSK);			
                      dk_set_led_on(0x00);
                      dk_set_led_on(0x01);                      
                      break;
              }
          }
      dk_set_leds_state(0x00, DK_ALL_LEDS_MSK);
      err = mqtt_disconnect(&client);
      if (err) {
              dk_set_leds_state(0x00, DK_ALL_LEDS_MSK);
              printk("Could not disconnect MQTT client. Error: %d\n", err);
      }
   }
}

void main(void)
{
    int err;

    //Read device name.
    int at_socket_fd = -1;
    at_socket_fd = socket(AF_LTE, 0, NPROTO_AT);
    if (at_socket_fd == -1) {
          printf("Creating at_socket failed\n");
          return -EFAULT;
    }
    const char* AT_CMD = "AT+CGSN\r\n";
    int cnt = send(at_socket_fd, AT_CMD, strlen(AT_CMD), 0);
    /* Read AT socket in blocking mode. */
    cnt = recv(at_socket_fd, DEVICE_NAME, sizeof(DEVICE_NAME), 0);
    close(at_socket_fd);
    char* pEnd = strstr(DEVICE_NAME, "\r");
    *pEnd = '\0';

    err = gx_uart_init("UART_0");
    if (err != 0) {
      printf("Failed to open serial port.\n");    
      return;
    }    

    printk("Broker: %s:%d\n", CONFIG_MQTT_BROKER_HOSTNAME, CONFIG_MQTT_BROKER_PORT);
    printk("Device name: %s\n", DEVICE_NAME);
    frame_rdy = false;
    connected = false;
        msg_init(&msg);

        err = dk_leds_init();
	if (err) {
		printk("Could not initialize leds, err code: %d\n", err);
	}

	err = dk_set_leds_state(DK_ALL_LEDS_MSK, 0);
	if (err) {
		printk("Could not set leds state, err code: %d\n", err);
	}
	err = dk_set_leds_state(0x00, DK_ALL_LEDS_MSK);
        err = gx_uart_init("UART_0");
        if (err != 0) {
          printf("Failed to open serial port.\n");    
          return;
        }
	modem_configure();
        err = try_to_connect(&client);
        printf("Modem Confiqured.\n");    
        k_thread_create(&my_thread_data2, my_stack_area2,
                                 K_THREAD_STACK_SIZEOF(my_stack_area2),
                                 mqtt_connectThread,
                                 NULL, NULL, NULL,
                                 MY_PRIORITY, 0, K_NO_WAIT);                                 
    while (1) {      
      k_sleep(500);
      if (frame_rdy){
        sendData();
      }       
    }
}

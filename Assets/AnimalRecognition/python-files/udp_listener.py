#!/usr/bin/env python3
import socket
import json
import logging
from datetime import datetime

# Set up logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

def start_udp_listener(host='127.0.0.1', port=5005):
    """Start UDP listener to receive messages"""
    try:
        # Create UDP socket
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.bind((host, port))
        
        logger.info(f"UDP Listener started on {host}:{port}")
        logger.info("Waiting for messages...")
        logger.info("Press Ctrl+C to stop")
        
        while True:
            try:
                # Receive data
                data, addr = sock.recvfrom(1024)
                
                # Decode and parse JSON
                message_str = data.decode('utf-8')
                message = json.loads(message_str)
                
                # Log the received message
                timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
                logger.info(f"[{timestamp}] Received from {addr}:")
                logger.info(f"  Animal: {message.get('animal', 'Unknown')}")
                logger.info(f"  Confidence: {message.get('confidence', 0):.2f}")
                logger.info(f"  Timestamp: {message.get('timestamp', 'Unknown')}")
                logger.info(f"  Raw message: {message_str}")
                logger.info("-" * 50)
                
            except json.JSONDecodeError as e:
                logger.error(f"Invalid JSON received: {e}")
                logger.error(f"Raw data: {data}")
            except Exception as e:
                logger.error(f"Error processing message: {e}")
                
    except KeyboardInterrupt:
        logger.info("UDP Listener stopped by user")
    except Exception as e:
        logger.error(f"UDP Listener error: {e}")
    finally:
        sock.close()

if __name__ == "__main__":
    start_udp_listener() 
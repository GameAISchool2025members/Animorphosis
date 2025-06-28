#!/usr/bin/env python3
import asyncio
import websockets
import json
import socket
import logging
from datetime import datetime

# Set up logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class UDPProxy:
    def __init__(self):
        self.udp_socket = None
        self.target_host = "127.0.0.1"
        self.target_port = 5005
        self.clients = set()
        
    def setup_udp_socket(self):
        """Create UDP socket for sending messages"""
        try:
            self.udp_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            logger.info(f"UDP socket created for {self.target_host}:{self.target_port}")
        except Exception as e:
            logger.error(f"Error creating UDP socket: {e}")
            
    def send_udp_message(self, message):
        """Send message via UDP"""
        if not self.udp_socket:
            self.setup_udp_socket()
            
        try:
            # Convert message to JSON string and encode
            message_str = json.dumps(message)
            message_bytes = message_str.encode('utf-8')
            
            # Send via UDP
            self.udp_socket.sendto(message_bytes, (self.target_host, self.target_port))
            logger.info(f"UDP message sent to {self.target_host}:{self.target_port}: {message_str}")
            return True
        except Exception as e:
            logger.error(f"Error sending UDP message: {e}")
            return False

# Global UDP proxy instance
udp_proxy = UDPProxy()

async def handle_websocket(websocket, path):
    """Handle WebSocket connections"""
    client_id = id(websocket)
    udp_proxy.clients.add(websocket)
    logger.info(f"Client {client_id} connected. Total clients: {len(udp_proxy.clients)}")
    
    try:
        async for message in websocket:
            try:
                data = json.loads(message)
                logger.info(f"Received from client {client_id}: {data}")
                
                if data.get('type') == 'config':
                    # Update UDP target configuration
                    udp_proxy.target_host = data.get('host', '127.0.0.1')
                    udp_proxy.target_port = data.get('port', 8888)
                    logger.info(f"UDP target updated to {udp_proxy.target_host}:{udp_proxy.target_port}")
                    
                    # Send confirmation
                    await websocket.send(json.dumps({
                        'type': 'config_ack',
                        'status': 'success',
                        'target': f"{udp_proxy.target_host}:{udp_proxy.target_port}"
                    }))
                    
                elif data.get('type') == 'udp_message':
                    # Forward message to UDP
                    success = udp_proxy.send_udp_message(data)
                    
                    # Send confirmation back to client
                    response = {
                        'type': 'udp_ack',
                        'success': success,
                        'timestamp': datetime.now().isoformat()
                    }
                    await websocket.send(json.dumps(response))
                    
                else:
                    logger.warning(f"Unknown message type from client {client_id}: {data.get('type')}")
                    
            except json.JSONDecodeError as e:
                logger.error(f"Invalid JSON from client {client_id}: {e}")
                await websocket.send(json.dumps({
                    'type': 'error',
                    'message': 'Invalid JSON format'
                }))
                
    except websockets.exceptions.ConnectionClosed:
        logger.info(f"Client {client_id} connection closed")
    except Exception as e:
        logger.error(f"Error handling client {client_id}: {e}")
    finally:
        udp_proxy.clients.discard(websocket)
        logger.info(f"Client {client_id} disconnected. Total clients: {len(udp_proxy.clients)}")

async def main():
    """Main server function"""
    host = "localhost"
    port = 8004
    
    logger.info(f"Starting WebSocket server on {host}:{port}")
    logger.info("This server acts as a bridge between web clients and UDP")
    
    # Start WebSocket server
    async with websockets.serve(handle_websocket, host, port):
        logger.info(f"WebSocket server running on ws://{host}:{port}")
        logger.info("Waiting for connections...")
        
        # Keep the server running
        await asyncio.Future()  # Run forever

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        logger.info("Server stopped by user")
    except Exception as e:
        logger.error(f"Server error: {e}") 
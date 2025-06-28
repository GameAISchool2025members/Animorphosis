#!/usr/bin/env python3
import http.server
import socketserver
import json
import socket
import logging
import urllib.parse
from datetime import datetime

# Set up logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class UDPForwarder:
    def __init__(self):
        self.udp_socket = None
        self.target_host = "127.0.0.1"
        self.target_port = 5005
        
    def setup_udp_socket(self):
        """Create UDP socket for sending messages"""
        try:
            self.udp_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            logger.info(f"UDP socket created for {self.target_host}:{self.target_port}")
        except Exception as e:
            logger.error(f"Error creating UDP socket: {e}")
            
    def send_udp_message(self, message):
        """Send a string message via UDP"""
        if not self.udp_socket:
            self.setup_udp_socket()
            
        try:
            # Encode the string message for UDP
            message_bytes = str(message).encode('utf-8')
            
            # Send via UDP
            self.udp_socket.sendto(message_bytes, (self.target_host, self.target_port))
            logger.info(f"UDP message sent to {self.target_host}:{self.target_port}: {message}")
            return True
        except Exception as e:
            logger.error(f"Error sending UDP message: {e}")
            return False

# Global UDP forwarder instance
udp_forwarder = UDPForwarder()

class UDPHTTPRequestHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type')
        super().end_headers()

    def do_OPTIONS(self):
        self.send_response(200)
        self.end_headers()

    def do_GET(self):
        """Handle GET requests for UDP forwarding"""
        parsed_path = urllib.parse.urlparse(self.path)
        if parsed_path.path == '/udp':
            try:
                query_components = urllib.parse.parse_qs(parsed_path.query)
                data = {k: v[0] for k, v in query_components.items()}
                logger.info(f"Received GET request with data: {data}")
                
                # Handle different message types
                if data.get('type') == 'config':
                    # Update UDP target configuration
                    udp_forwarder.target_host = data.get('host', '127.0.0.1')
                    udp_forwarder.target_port = int(data.get('port', 8888))
                    logger.info(f"UDP target updated to {udp_forwarder.target_host}:{udp_forwarder.target_port}")
                    
                    response = {
                        'status': 'success',
                        'message': f'UDP target set to {udp_forwarder.target_host}:{udp_forwarder.target_port}'
                    }
                    
                elif data.get('type') == 'udp_message':
                    # Forward animal string to UDP
                    animal = data.get('animal')
                    if animal:
                        success = udp_forwarder.send_udp_message(animal)
                        message = 'UDP message sent'
                    else:
                        success = False
                        message = "Failed to send UDP message: 'animal' not provided"

                    response = {
                        'status': 'success' if success else 'error',
                        'message': message,
                        'timestamp': datetime.now().isoformat()
                    }
                    
                else:
                    response = {
                        'status': 'error',
                        'message': 'Unknown or missing message type in query string'
                    }
                
                # Send response
                self.send_response(200)
                self.send_header('Content-type', 'application/json')
                self.end_headers()
                self.wfile.write(json.dumps(response).encode('utf-8'))
                
            except Exception as e:
                logger.error(f"Error handling GET request: {e}")
                self.send_response(500)
                self.send_header('Content-type', 'application/json')
                self.end_headers()
                self.wfile.write(json.dumps({
                    'status': 'error',
                    'message': str(e)
                }).encode('utf-8'))
        else:
            # Handle other GET requests as file serving
            super().do_GET()

    def do_POST(self):
        """Handle POST requests for UDP forwarding"""
        if self.path == '/udp':
            try:
                # Get content length
                content_length = int(self.headers['Content-Length'])
                post_data = self.rfile.read(content_length)
                
                # Parse JSON data
                data = json.loads(post_data.decode('utf-8'))
                logger.info(f"Received POST data: {data}")
                
                # Handle different message types
                if data.get('type') == 'config':
                    # Update UDP target configuration
                    udp_forwarder.target_host = data.get('host', '127.0.0.1')
                    udp_forwarder.target_port = data.get('port', 8888)
                    logger.info(f"UDP target updated to {udp_forwarder.target_host}:{udp_forwarder.target_port}")
                    
                    # Send success response
                    response = {
                        'status': 'success',
                        'message': f'UDP target set to {udp_forwarder.target_host}:{udp_forwarder.target_port}'
                    }
                    
                elif data.get('type') == 'udp_message':
                    # Forward animal string to UDP
                    animal = data.get('animal')
                    if animal:
                        success = udp_forwarder.send_udp_message(animal)
                        message = 'UDP message sent'
                    else:
                        success = False
                        message = "Failed to send UDP message: 'animal' not provided"

                    response = {
                        'status': 'success' if success else 'error',
                        'message': message,
                        'timestamp': datetime.now().isoformat()
                    }
                    
                else:
                    response = {
                        'status': 'error',
                        'message': 'Unknown message type'
                    }
                
                # Send response
                self.send_response(200)
                self.send_header('Content-type', 'application/json')
                self.end_headers()
                self.wfile.write(json.dumps(response).encode('utf-8'))
                
            except Exception as e:
                logger.error(f"Error handling POST request: {e}")
                self.send_response(500)
                self.send_header('Content-type', 'application/json')
                self.end_headers()
                self.wfile.write(json.dumps({
                    'status': 'error',
                    'message': str(e)
                }).encode('utf-8'))
        else:
            # Handle other POST requests as file serving
            super().do_POST()

if __name__ == "__main__":
    PORT = 8005
    MAX_PORT_ATTEMPTS = 10  # Try up to 10 different ports
    
    # Change to the directory containing the files
    import os
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    
    # Try to find an available port
    for port_attempt in range(MAX_PORT_ATTEMPTS):
        current_port = PORT + port_attempt
        try:
            with socketserver.TCPServer(("", current_port), UDPHTTPRequestHandler) as httpd:
                print(f"UDP HTTP server running at http://localhost:{current_port}")
                print("This server forwards HTTP POST or GET requests to UDP")
                print("POST to /udp with JSON data to send UDP messages")
                print("GET from /udp with URL parameters to send UDP messages")
                print("Press Ctrl+C to stop the server")
                httpd.serve_forever()
                break  # Successfully started server, exit the loop
        except OSError as e:
            if e.errno == 48:  # Address already in use
                print(f"Port {current_port} is already in use, trying next port...")
                if port_attempt == MAX_PORT_ATTEMPTS - 1:
                    print(f"Error: Could not find an available port after {MAX_PORT_ATTEMPTS} attempts")
                    print("Please check if another instance is running or manually specify a port")
                    exit(1)
                continue
            else:
                # Re-raise other OSErrors
                raise 
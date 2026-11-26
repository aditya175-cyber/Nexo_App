"""WebSocket IPC server — bridges Python backend to C# frontend."""

import asyncio
import json
import websockets
from modules.system_monitor import SystemMonitor


class IPCServer:
    def __init__(self, host: str = "localhost", port: int = 9123):
        self.host = host
        self.port = port
        self.monitor = SystemMonitor()
        self.clients: set = set()

    async def _handler(self, websocket):
        """Handle a single client connection."""
        self.clients.add(websocket)
        print(f"[IPC] Client connected ({len(self.clients)} total)")
        try:
            async for message in websocket:
                # Handle incoming commands from C# frontend
                try:
                    data = json.loads(message)
                    cmd = data.get("type")
                    if cmd == "ping":
                        await websocket.send(json.dumps({"type": "pong"}))
                except json.JSONDecodeError:
                    pass
        except websockets.ConnectionClosed:
            pass
        finally:
            self.clients.discard(websocket)
            print(f"[IPC] Client disconnected ({len(self.clients)} total)")

    async def _broadcast_loop(self):
        """Push system data to all connected clients every second."""
        while True:
            if self.clients:
                stats = self.monitor.get_stats()
                message = json.dumps({
                    "type": "system",
                    **stats,
                })

                dead = set()
                for client in self.clients:
                    try:
                        await client.send(message)
                    except Exception:
                        dead.add(client)
                self.clients -= dead

            await asyncio.sleep(1)

    async def start(self):
        """Start the WebSocket server and broadcast loop."""
        async with websockets.serve(self._handler, self.host, self.port):
            print(f"[IPC] Listening on ws://{self.host}:{self.port}")
            await self._broadcast_loop()


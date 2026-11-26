"""Nexo Backend Service — Entry Point."""

import asyncio
import sys
import os

# Ensure imports work from project root
sys.path.insert(0, os.path.dirname(__file__))

from core.ipc_server import IPCServer


async def main():
    print("=" * 42)
    print("   Nexo Backend Service")
    print("   ws://localhost:9123")
    print("=" * 42)

    server = IPCServer()
    await server.start()


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nBackend stopped.")


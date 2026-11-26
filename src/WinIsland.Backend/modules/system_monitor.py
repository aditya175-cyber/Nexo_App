"""Real-time system monitoring via psutil."""

import psutil


class SystemMonitor:
    def __init__(self):
        # Prime cpu_percent — first call always returns 0.0
        psutil.cpu_percent(interval=None)

    def get_stats(self) -> dict:
        """Return current CPU, RAM, and network stats."""
        cpu = psutil.cpu_percent(interval=None)
        ram = psutil.virtual_memory()

        return {
            "cpu": round(cpu, 1),
            "ram": round(ram.percent, 1),
            "ram_used": round(ram.used / (1024 ** 3), 1),
            "ram_total": round(ram.total / (1024 ** 3), 1),
        }


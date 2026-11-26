# Product Requirements Document (PRD): Windows Dynamic Island (WinIsland)

## 1. Ringkasan Eksekutif (Executive Summary)

**Nexo** adalah aplikasi utilitas produktivitas berbasis desktop untuk Windows 10/11 yang mengadaptasi konsep *Dynamic Island* menjadi pusat kendali interaktif di bagian atas layar. Aplikasi ini menggabungkan performa antarmuka native yang halus menggunakan **C#** dengan fleksibilitas pemrosesan data dan otomasi latar belakang menggunakan **Python**.

## 2. Tujuan Produk & Target Pengguna

* **Tujuan:** Menyediakan akses cepat (*one-glance & one-click*) ke berbagai alat produktivitas, pemantauan sistem, dan kontrol media tanpa mengganggu alur kerja (*workflow*) utama pengguna.

* **Target Pengguna:** Pengguna Windows (*power users*, developer, kreator konten, dan pekerja kantoran) yang menginginkan manajemen *multitasking* yang efisien dan estetika desktop modern.

## 3. Arsitektur Teknologi (Tech Stack)

Aplikasi ini menggunakan arsitektur *Hybrid* (Frontend/UI dan Backend/Worker):

1. **Frontend & UI Layer (C#):**

   * **Framework:** WPF (Windows Presentation Foundation) atau WinUI 3 (.NET 8+).

   * **Tanggung Jawab:** Render animasi *spring/morphing* Dynamic Island, manajemen *always-on-top window*, transparansi *acrylic/mica*, menangkap *global hotkeys*, dan integrasi Windows API (SMTC untuk media, Win32 API untuk manajemen jendela).

2. **Backend Service & Logic Layer (Python):**

   * **Versi:** Python 3.11+.

   * **Tanggung Jawab:** Pemantauan hardware/CPU (`psutil`), pengambilan data cuaca (*API fetching*), pemrosesan manipulasi teks (*Text Tool*), eksekusi skrip perintah (*Command*), dan manajemen penyimpanan lokal (SQLite/JSON).

3. **Inter-Process Communication (IPC):**

   * Komunikasi dua arah berlatensi rendah antara C# dan Python menggunakan **Named Pipes**, **gRPC**, atau **Local WebSockets** (`localhost`).

## 4. Spesifikasi Fitur Fungsional (Functional Requirements)

Antarmuka WinIsland memiliki tiga status utama: **Compact Idle** (kapsul kecil), **Expanded Widget** (tampilan penuh saat diklik/di-hover), dan **Live Activity / Toast** (notifikasi sementara).

### A. Status & Navigasi Utama

1. **Compact Idle:**

   * Tampilan default berbentuk kapsul minimalis di tengah atas layar.

   * Menampilkan indikator mikro (jam kecil, gelombang audio saat musik menyala, atau persentase CPU).

   * Mendukung gestur *hover* untuk pratinjau cepat dan *klik* untuk membuka mode *Expanded*.

2. **Home (Dashboard Utama):**

   * Layar pusat saat Island diperluas, menampilkan ringkasan waktu, tanggal, cuaca singkat, serta *dock* ikon untuk berpindah antar modul fitur.

3. **Settings:**

   * Pengaturan posisi (offset X/Y, dukungan multi-monitor), tema (Dark/Light/System), transparansi, sensitivitas *hover*, dan *startup with Windows*.

### B. Utilitas Produktivitas

4. **Calendar:**

   * Menampilkan kalender bulanan mini dan daftar agenda/jadwal hari ini.

   * Mendukung sinkronisasi lokal atau integrasi kalender sistem.

5. **Clipboard Manager:**

   * Menyimpan riwayat teks dan gambar yang disalin (*copy history*) secara *real-time*.

   * Fitur *pin* item penting, pencarian cepat, dan klik untuk langsung menempel (*paste*).

6. **Notes (Quick Notes):**

   * Scratchpad minimalis untuk mencatat ide cepat dengan penyimpanan otomatis (*auto-save*) ke file lokal/SQLite oleh backend Python.

7. **Text Tool:**

   * Alat manipulasi teks instan dari clipboard atau input langsung: ubah huruf kapital (*UPPERCASE, lowercase, Title Case*), hitung jumlah kata/karakter, *JSON formatter*, dan *Base64 encode/decode*.

8. **Timer & Stopwatch:**

   * Penghitung waktu mundur (*countdown*) dan *stopwatch* dengan preset cepat (misal: Pomodoro 25 menit).

   * Saat diminimalkan ke *Compact Idle*, sisa waktu tetap berjalan dan terlihat di kapsul.

### C. Kontrol Sistem & Otomasi

 9. **Music (Media Controller):**

    * Integrasi dengan Windows System Media Transport Controls (SMTC) untuk mendeteksi pemutar media aktif (Spotify, YouTube di browser, Apple Music, dll.).

    * Menampilkan *album art*, judul lagu, artis, *visualizer* mini, serta tombol *Play/Pause, Next, Previous*.

10. **CPU & System Monitor:**

    * Backend Python (`psutil`) memantau penggunaan CPU, RAM, suhu (jika didukung), dan kecepatan jaringan (*upload/download*) secara *real-time*.

    * Menampilkan grafik *sparkline* mini pada UI C#.

11. **System Controls:**

    * Kontrol cepat perangkat keras: *Slider* volume suara, kecerahan layar (*brightness*), *toggle* Wi-Fi/Bluetooth, serta tombol cepat *Lock, Sleep, Restart, Shutdown*.

12. **Open Splits (Window Snapper):**

    * Manajemen tata letak jendela (*window management*) cepat. Pengguna dapat memilih jendela aplikasi yang sedang terbuka dan langsung membaginya ke posisi layar (*Left Half, Right Half, Grid 4x4*) menggunakan Win32 API.

13. **Command (Quick Launcher / Terminal Runner):**

    * Baris perintah cepat untuk menjalankan perintah terminal/PowerShell, membuka aplikasi tertentu, atau menjalankan skrip kustom Python secara instan.

### D. Notifikasi & Informasi Eksternal

14. **Toast (Dynamic Notifications):**

    * Menangkap notifikasi sistem Windows atau notifikasi internal aplikasi dan menampilkannya dengan animasi meluas yang halus dari kapsul Dynamic Island.

15. **Weather:**

    * Backend Python mengambil data cuaca lokal secara berkala (via OpenWeatherMap / Open-Meteo API).

    * Menampilkan suhu saat ini, ikon kondisi cuaca, kelembaban, dan prakiraan beberapa jam ke depan.

## 5. Persyaratan Non-Fungsional (Non-Functional Requirements)

* **Performa:** Penggunaan RAM gabungan (C# + Python) maksimal di bawah **150 MB** saat *idle*, dan penggunaan CPU < **2%**.

* **Animasi:** Framerate animasi transisi kapsul minimal **60 FPS** dengan kurva *easing/spring* yang responsif.

* **Stabilitas:** Jika proses Python *crash*, aplikasi utama C# harus tetap berjalan dan otomatis melakukan *restart* pada layanan Python di latar belakang.

## 6. Struktur Proyek yang Disarankan (Untuk AI Antigravity)

```
WinIsland/
├── src/
│   ├── WinIsland.UI/                 # Project C# (WPF / WinUI 3)
│   │   ├── Views/                    # XAML Views (IslandWindow, HomeView, MusicView, dll.)
│   │   ├── ViewModels/               # MVVM Logic
│   │   ├── Services/                 # IPC Client, Windows Media API, Win32 Window Manager
│   │   └── App.xaml
│   └── WinIsland.Backend/            # Project Python
│       ├── core/                     # IPC Server (Named Pipes / WebSocket)
│       ├── modules/                  # system_monitor.py, weather.py, text_tool.py, command.py
│       ├── database/                 # SQLite handler untuk Notes & Clipboard
│       └── main.py                   # Entry point Python worker
├── docs/
│   └── prd.md
└── README.md

```

## 7. Tahapan Pengembangan (Milestones)

1. **Fase 1 (Core & IPC):** Membangun jendela kapsul C# (*Compact Idle* & animasi *expand*), serta menghubungkannya dengan Python via Named Pipes/WebSocket.

2. **Fase 2 (System & Media):** Implementasi fitur *Music*, *CPU*, *System Controls*, dan *Toast*.

3. **Fase 3 (Productivity):** Implementasi *Clipboard*, *Notes*, *Text Tool*, *Timer*, dan *Calendar*.

4. **Fase 4 (Advanced & Polish):** Implementasi *Open Splits*, *Command*, *Weather*, *Settings*, serta optimasi memori.
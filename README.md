# 🏋️‍♂️ SportLab — Sports Club Management Web Application

**SportLab** is a full-stack web application built on the ASP.NET Core MVC architecture, designed to automate sports gym operations and streamline interactions between clients, trainers, and administration.

---

## 🚀 Key Features

### 👥 Role-Based Access Control (RBAC)
* **Client:** View available tariffs, purchase fitness gear in the internal shop, book workout slots via an interactive calendar, track weight progress, and communicate directly with assigned trainers.
* **Trainer:** Manage personal work schedules, configure available slots, view registered clients, and conduct consultations via the internal messaging system.
* **Administrator:** Global system administration (user account moderation, blocking/unblocking accounts), tariff configuration, shop inventory control, full schedule oversight, and broadcast messaging to trainers (`BROADCAST_TRAINERS`).

### 💡 Core System Modules
* **Messaging Module (`Messages`):** Dual-pane chat interface featuring dynamic message loading via AJAX, unread message counter, and role-based contact routing.
* **Interactive Schedule (`WorkoutSlots`):** Visual slot grid with color-coded availability ("Free" vs. "Booked") and one-click session booking.
* **Store & Shopping Cart:** Product catalog with category filtering, dynamic sorting, and checkout simulation.
* **Progress Tracker:** Visual biometrics journal for monitoring personal weight changes over time.

---

## 🛠 Tech Stack

* **Backend:** C#, .NET 8 / ASP.NET Core MVC
* **Database & ORM:** MS SQL Server Express, Entity Framework Core (Code First)
* **Frontend:** HTML5, CSS3 (Tailwind CSS), JavaScript (AJAX / Fetch API), Razor Views
* **Web Server:** Kestrel
* **Version Control:** Git, GitHub

---

## ⚙️ Setup and Installation

1. **Clone the repository:**
   ```bash
   git clone [https://github.com/makovska-nastya/SportApp.git](https://github.com/makovska-nastya/SportApp.git)
   cd SportLab

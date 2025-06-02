# 🐳 WSL2 Docker Integration Installer

This project provides a simple and fully automated installer for setting up a Docker development environment using **WSL2** (Windows Subsystem for Linux) on Windows. The installer:

* Installs WSL2 and Ubuntu
* Sets up Docker and Docker Compose within the WSL2 Ubuntu environment
* Exposes the Docker daemon via TCP to be natively accessible from Windows
* Installs the Docker CLI on Windows and configures the environment (including `PATH` and `DOCKER_HOST`)

---

## ✅ Features

* 📦 Automatic setup of WSL2 and Ubuntu
* 🐳 Full Docker daemon configuration in WSL2
* 🔁 TCP-based Docker access from Windows (`docker ps`)
* 🔧 Adds Docker CLI to Windows with `DOCKER_HOST` environment variable
* 🔐 Adds required firewall and port proxy rules
* 🪄 Scheduled task for Docker daemon auto-start

---

## 🚀 Quick Start

1. **Run the Installer (as Administrator)**
   Launch the provided `.exe` installer. It will:

   * Install WSL2
   * Set up Ubuntu and Docker inside WSL2
   * Configure Docker access via TCP
   * Download the Docker CLI and set up your environment

2. **Restart Your Computer**
   Required to apply environment variables (`PATH`, `DOCKER_HOST`) globally.

3. **Use Docker from Windows!**
   Open PowerShell, CMD, or Terminal and run:

   ```bash
   docker ps
   ```

---

## ⚙️ How It Works

1. **WSL Installation**

   * Ensures WSL2 is enabled and Ubuntu is installed
   * Disables systemd to avoid overhead

2. **Docker Daemon Setup**

   * Installs Docker and Docker Compose in WSL
   * Configures the daemon to listen on both:

     * Unix socket (`/var/run/docker.sock`)
     * TCP (`tcp://0.0.0.0:2375`)

3. **Scheduled Task**

   * A Windows Scheduled Task launches `dockerd` inside WSL on user login

4. **Networking**

   * A firewall rule and port proxy forward Docker from WSL to Windows:

     ```bash
     netsh advfirewall firewall add rule ...
     netsh interface portproxy add v4tov4 ...
     ```

5. **Docker CLI**

   * Downloads the official Docker CLI from Docker's GitHub
   * Extracts and places it into `C:\sw\DockerCLI\docker`
   * Adds the folder to the Windows `PATH`
   * Sets `DOCKER_HOST=tcp://localhost:2375` for native use

---

## 🔧 Customization

You can modify:

* The port used for Docker (`2375` by default)
* Whether the Docker daemon starts automatically (via Scheduled Task)
* The WSL distribution name (defaults to `docker-ubuntu`)

---

## 🛠 Requirements

* Windows 10 2004+ or Windows 11 (only tested on Win 11)
* Admin privileges during installation
* Internet connection (for package download)

---

## 💡 Troubleshooting

* **Docker not found?**
  Make sure you restarted Windows after installation.

* **`docker ps` says daemon is unreachable?**
  Ensure WSL is running and that the daemon was started (`dockerd` inside WSL).

* **Need to uninstall?**
  You can manually delete the scheduled task and firewall/portproxy rules:

  ```bash
  rd /s /q "C:\sw\DockerCLI" <-- Deletes Docker CLI folder>
  wsl --unregister {DistribuationName} <-- Unregisters WSL distribution>
  schtasks /Delete /TN "DockerStart" /F <-- Deletes scheduled task>
  netsh interface portproxy delete v4tov4 listenport=2375 listenaddress=0.0.0.0 <-- Deletes port proxy>
  ```

---

## 📜 License

This project is provided under the MIT License.

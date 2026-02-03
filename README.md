# Reality Toolkit - uxmanager

![com.realitytoolkit.uxmanager](https://github.com/realitycollective/realitycollective.logo/blob/main/RealityToolkit/RepoBanners/com.realitytoolkit.uxmanager.png?raw=true)

The uxmanager module for the [Reality Toolkit](https://realitytoolkit.realitycollective.net/).

[![openupm](https://img.shields.io/npm/v/com.realitytoolkit.uxmanager?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.realitytoolkit.uxmanager/) [![Discord](https://img.shields.io/discord/597064584980987924.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/hF7TtRCFmB)
[![Publish main branch and increment version](https://github.com/realitycollective/com.realitytoolkit.uxmanager/actions/workflows/main-publish.yml/badge.svg)](https://github.com/realitycollective/com.realitytoolkit.uxmanager/actions/workflows/main-publish.yml)
[![Publish development branch on Merge](https://github.com/realitycollective/com.realitytoolkit.uxmanager/actions/workflows/development-publish.yml/badge.svg)](https://github.com/realitycollective/com.realitytoolkit.uxmanager/actions/workflows/development-publish.yml)
[![Build and test UPM packages for platforms, all branches except main](https://github.com/realitycollective/com.realitytoolkit.uxmanager/actions/workflows/development-buildandtestupmrelease.yml/badge.svg)](https://github.com/realitycollective/com.realitytoolkit.uxmanager/actions/workflows/development-buildandtestupmrelease.yml)

## Installation

Make sure to always use the same source for all toolkit modules. Avoid using different installation sources within the same project. We provide the following ways to install Reality Toolkit modules:

### Method 1: Using Package Manager for git users

1. Open the Package Manager using the Window menu -> Package Manager

2. Inside the Package Manager, click on the "+" button on the top left and select "Add package from git URL..."

3. Input the following URL: <https://github.com/realitycollective/com.realitytoolkit.uxmanager.git> and click "Add".

### Method 2: OpenUPM

```text
    openupm add com.realitytoolkit.uxmanager
```

### Method 3: Unity Asset Store

This option will be available soon.

## Getting Started

Check the ["Getting Started"](https://realitytoolkit.realitycollective.net/) documentation for the Reality Toolkit and to learn more about this module.

---

## Services

### UX Manager Service

The UX Manager is a comprehensive framework for building user experience systems in Unity using **UI Toolkit exclusively** (no uGUI/Canvas).

**Features:**

- Centralized screen management with lifecycle control
- MVVM-pattern screens and handlers for clean architecture
- Event-driven design for decoupled component communication
- Service Framework integration via Reality Collective
- 100% UI Toolkit with programmatic UI generation
- Automatic screen transitions and state management

**Use When:**

- Building complex, multi-screen applications
- You need clean separation between UI logic and business logic
- You want automatic screen lifecycle management
- Your project uses UI Toolkit for all interfaces

📖 **[Full Documentation](Documentation~/com.realitycollective.uxmanager.md)**

---

### Localization Service

The Localization Service provides multi-language support with automatic fallback, runtime locale switching, and culture-aware formatting.

**Features:**

- Multi-language support with JSON catalogs
- Two loading strategies: **Resources** (bundled) or **Addressables** (on-demand)
- Automatic fallback to default locale
- String keys and enum localization
- Format string support with parameters
- Culture-aware date/time/number formatting
- Runtime locale switching with UI refresh events
- Comprehensive key management and validation

**Loading Strategies:**

- **Resources**: Catalogs bundled with app (simple, small projects, 2-4 locales)
- **Addressables**: On-demand loading with remote updates (production, 5+ locales, frequent updates)

**Use When:**

- Supporting multiple languages
- Need automatic fallback handling
- Want culture-specific date/time formatting
- Building international applications

📖 **[Full Documentation](Documentation~/com.realitycollective.localizationmanager.md)**

---

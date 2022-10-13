# Spaceship Demo

![](https://blogs.unity3d.com/wp-content/uploads/2019/08/image10.png)

Spaceship Demo is a AAA Playable First person demo showcasing effects made with Visual Effect Graph and rendered with High Definition Render Pipeline.

**[DOWNLOAD LATEST RELEASE HERE](https://github.com/Unity-Technologies/SpaceshipDemo/releases/latest)**

For update information, see the [Changelog](https://github.com/Unity-Technologies/SpaceshipDemo/blob/master/CHANGELOG.md).

## Requirements

In order to download and run the latest Spaceship demo project, make sure you have the following
* [Github Desktop](https://desktop.github.com/) or [Git For Windows](https://git-scm.com/download/win) + [Git LFS](https://git-lfs.github.com/) (Required for Cloning the Repository) or any other git client.
* Unity 2021.2.1f1 or newer (See each release notes in [changelog](https://github.com/Unity-Technologies/SpaceshipDemo/blob/master/CHANGELOG.md) for version requirements)

## How to Download/Install

### Method 1 : Clone the repository

**Important Note**: This repository uses **Git-LFS** to store large files. In order to get the data correctly you need to [install Git-LFS](https://git-lfs.github.com/) before starting to clone the repository. **Do NOT use the Download ZIP button as it will not get the LFS files correctly**

#### Using Github Desktop

If you have [Github Desktop](https://desktop.github.com/) installed :  use the **Clone or Download** green button, then select **Open in Desktop**.

#### Using Git Command Line (or another Git Client)

You can clone this repository and start opening directly the project using the following command : `git clone https://github.com/Unity-Technologies/SpaceshipDemo.git`

### Method 2 : Download in Releases page

You can also download project archives in the [Releases](https://github.com/Unity-Technologies/SpaceshipDemo/releases) tab. These zip files contains the full project for a one-time download without Git. 

## NVIDIA perf measurements

### Console commands 

Currently it's expected for the menu mode to have artifacts. That's because of how cameras are managed in this project, I wasn't able to find a suitable solution yet.

Command line arguments:

* "-benchmark": - enables benchmark run starting with the scripted scene and prints out perf report in the end

* "-screenshots": - enables screenshot dumping every 10-20 seconds

* "-reportpath": - sets the path where reports (JSON/HTML) should be placed. If not specified, the default value is "MyDocuments/Spaceship Demo"

* "-quality": - sets the overall quality:

		"low",

		"high",

		"ultra"
		
If not set, the serialized value from GUI Options is taken (which may have changed on the previous run if this option was specified)

* "-upsamplingmethod": sets the upsampling method from the following list:

		"CatmullRom",

		"CAS",

		"TAAU",

		"FSR",

		"DLSS"

the call should look like this: "-upsamplingmethod DLSS". If not set, the serialized value from GUI Options is taken (which may have changed on the previous run if this option was specified).

**Note that if the DLSS is enabled, the fallback AA method won't get activated because the Unity engine forces it off since the DLSS does both the upscaling and anti-aliasing.**

* "-aa" sets anti-aliasing method for fallback rendering (for those upscaling methods that do not support antialiasing, that's non-DLSS methods):

		"none",

		"FXAA",

		"TAA",

		"SMAA"

If not set, the serialized value from GUI Options is taken (which may have changed on the previous run if this option was specified).

* "-nofinalblit": if present, disables blitting the offscreen render target to the screen, saving CPU/GPU time. Screen is expected to be black with UI text apparing on it if activated.

* "-offscreenres": Sets the offscreen resolution. "-offscreenres 4K" sets the resolution to 4K. "-offscreenres 4000x2000" sets custom resolution (WxH). Predefined parameters are: 4K, 2K, FHD, 2160p, 1440p, 1080p. So just add the predefined paramtere to -offscreenres to get the appropriate resolution, like this: "-offscreenres 1440p" or "-offscreenres 2K". If not set, the serialized value from GUI Options is taken (which may have changed on the previous run if this option was specified)

* "-screenpercentage": Sets the screenpercentage in percent integers. For example to set half-resolution rendering add "-screenpercentage 50". To set the DLSS into DLAA mode use "-screenpercentage 100". If not set, the serialized value from GUI Options is taken (which may have changed on the previous run if this option was specified)
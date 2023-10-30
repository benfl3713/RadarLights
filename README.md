# RadarLights

Welcome to RadarLights, a project to create a led matrix software to display realtime aircraft positions 
using the [PiAware program](https://www.flightaware.com/adsb/piaware/) as a data source.

This project is still in it's infancy so be aware there may be bugs and missing features.

## Hardware

1. Raspberry Pi 4+ (Controlling led matrix displays is very CPU intensive so you will want a dedicated Pi for this)
2. Led Matrix Display (Tested using 4 chained [64x64 RGB Led Matrix](https://thepihut.com/products/rgb-full-colour-led-matrix-panel-2-5mm-pitch-64x64-pixels), however most sizes should be compatible)
3. An led matrix controller for the raspberry pi. **Currently only the [Adafruit RGB Matrix Bonnet](https://www.adafruit.com/product/3211) is supported.**
4. A power supply for the led matrix display. **This is not optional, the Pi cannot power the display.**

## Installation

Please note, you will need to be running your own PiAware server to use this software. You can read more here: <https://uk.flightaware.com/adsb/piaware/>


1. Install the latest version of Raspbian on your Raspberry Pi. (Any linux distro should work but this is the only one tested)
2. I highly recommend following this tutorial to get your led matrix display working with some demos: <https://learn.adafruit.com/adafruit-rgb-matrix-bonnet-for-raspberry-pi/driving-matrices> (This software uses the same libraries under the hood so it is a good validation step)
3. The above guide also explains how to modify the HAT slightly by soldering on a wire between 2 points to put the HAT into Quality mode. This is reduces flickering on the display.
4. Download the latest release of RadarLights from the [releases page](https://github.com/benfl3713/RadarLights/releases) and extract it to a folder on your Pi.
5. Open a terminal and navigate to the folder you extracted the release to.
6. You will need to create a `appsettings.user.json` file to configure a few settings
```json
{
    "PiAwareServer": "http://raspberrypi:8080",
    "HomeLatitude": <Your latitude>,
    "HomeLongitude": <Your longitude>,
}
```   
The home latitude and longitude are used to plot the aircraft positions relative to your home being the centre of the display.

7. You can now run RadarLights making sure to run as root so it can access the led matrix display.
```bash
sudo ./RadarLights
```

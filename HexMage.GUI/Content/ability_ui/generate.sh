#!/bin/sh
convert spell_bg.png -modulate 100,100,0 spell_water_active_bg.png
convert spell_bg.png -modulate 100,100,80 spell_fire_active_bg.png
convert spell_bg.png -modulate 150,0 spell_air_active_bg.png
convert spell_bg.png spell_earth_active_bg.png

convert spell_water_active_bg.png -alpha set -channel A -evaluate set 30% spell_water_bg.png
convert spell_fire_active_bg.png  -alpha set -channel A -evaluate set 30% spell_fire_bg.png
convert spell_air_active_bg.png   -alpha set -channel A -evaluate set 30% spell_air_bg.png
convert spell_earth_active_bg.png -alpha set -channel A -evaluate set 30% spell_earth_bg.png

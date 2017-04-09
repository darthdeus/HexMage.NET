#!/bin/sh

for g in graphs/*.dot; do
	 gname=$(echo $g | sed "s/graphs\///")	
	 echo $gname
	dot -Tpng -o images/${gname}.png $g
done

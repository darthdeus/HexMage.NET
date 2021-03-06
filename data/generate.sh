#!/bin/sh

rm images/*.png

for g in graphs/*.dot; do
    echo "Generating $g"
    dot -Tpng -o images/$(basename -s .dot $g).png $g
done


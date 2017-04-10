#!/bin/sh

for g in graphs/*.dot; do
    dot -Tpng -o images/$(basename -s .dot $g).png $g
done


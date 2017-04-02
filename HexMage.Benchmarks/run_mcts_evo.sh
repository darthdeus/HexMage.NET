#!/bin/bash
nohup mono ./bin/Release/HexMage.Benchmarks.exe --factory=Mcts1 --EvolutionPrintModulo=1000 > /local/darthdeus/mcts-1 &
nohup mono ./bin/Release/HexMage.Benchmarks.exe --factory=Mcts10 --EvolutionPrintModulo=100 > /local/darthdeus/mcts-10 &
nohup mono ./bin/Release/HexMage.Benchmarks.exe --factory=Mcts100 --EvolutionPrintModulo=10 > /local/darthdeus/mcts-100 &
nohup mono ./bin/Release/HexMage.Benchmarks.exe --factory=Mcts1000 --EvolutionPrintModulo=1 > /local/darthdeus/mcts-1000 &

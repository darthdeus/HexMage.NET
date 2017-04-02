#!/bin/bash
nohup mono ./bin/Release/HexMage.Benchmarks.exe --EnableGnuPlot=false --MctsLogging=false --MctsBenchmark=false --factory=Mcts1 > /local/darthdeus/mcts-1.txt &
nohup mono ./bin/Release/HexMage.Benchmarks.exe --EnableGnuPlot=false --MctsLogging=false --MctsBenchmark=false --factory=Mcts10 > /local/darthdeus/mcts-10.txt &
nohup mono ./bin/Release/HexMage.Benchmarks.exe --EnableGnuPlot=false --MctsLogging=false --MctsBenchmark=false --factory=Mcts100 > /local/darthdeus/mcts-100.txt &
nohup mono ./bin/Release/HexMage.Benchmarks.exe --EnableGnuPlot=false --MctsLogging=false --MctsBenchmark=false --factory=Mcts1000 > /local/darthdeus/mcts-1000.txt &

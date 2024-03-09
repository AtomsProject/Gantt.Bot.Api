// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using Gantt.Bot.Scheduler.Benchmark;

BenchmarkRunner.Run<MonteCarloPertBenchmark>();
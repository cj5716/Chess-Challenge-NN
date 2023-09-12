# Chess Coding Challenge (C#) Example

### Compilation
To compile a self-contained UCI executable, first identify the appropriate runtime identifier:
- `win-x64` for Windows
- `linux-x64` for Linux
for other operating systems/errors, refer to the [docs](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog).
Then run
```
make OS=<runtime identifier>
```

### Search

#### Core
- Alpha-Beta Negamax
- Quiescence Search
- Iterative Deepening

#### Move ordering
- Transposition Table
- MVV-LVA for Captures

### Evaluation
- `768 -> 8x2 -> 1` Neural Network

# EasyMorph Test Task

A .NET console application that generates and processes large XML datasets in a streaming fashion using `XmlReader` and `XmlWriter`.

The application consists of two main components:

- **Generator** – generates folders containing XML files with random data.
- **Parser** – processes the generated XML files and produces an aggregated report.

The data used by the generator (store names and product information) is embedded into the application as compressed XML resources.

The application is a command-line tool and accepts command-line arguments. To display the available commands and options, run the application without any arguments.

## Build

```bash
dotnet build
```

## Implementation Highlights

- Streaming XML processing (`XmlReader` / `XmlWriter`)
- Low memory footprint
- GZip-compressed XML resources containing a list of major shopping malls in the USA and Canada, together with popular products and realistic price ranges
- Unit tests using xUnit
- Monetary values represented internally as integer cents
- XML element names are cached using `XmlNameTable` to reduce string allocations during parsing
- Parsing errors are written lazily to `errors.txt`, avoiding unnecessary memory allocations when no errors are present
- Report generation uses a single pass over the sorted data to minimize additional memory consumption
- A simple console progress bar provides real-time feedback during dataset generation and parsing

## Usage

### 1. Write the dataset to the directory
```bash
EasyMorph.TestTask generate [--work-dir <working directory>] [--stores <number of stores>] [--target-size <size in KB>]
```

### 2. Parse the dataset
```bash
EasyMorph.TestTask parse --work-dir <working directory>
```

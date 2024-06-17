# ADX to WAV and WAV to ADX Converter

This repository contains a C# library for converting ADX audio files to WAV format and vice versa. The original code was written in C and has been translated into C# compatible with the .NET Framework 3.5. The code includes additional logic to handle full file looping when converting from WAV to ADX format.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
- [Code Structure](#code-structure)
- [Acknowledgements](#acknowledgements)
- [License](#license)

## Features

- Convert ADX files to WAV format.
- Convert WAV files to ADX format with optional looping.
- Supports both mono and stereo audio channels.
- Preserves audio quality during conversion.

## Installation

To use this library, you need to have .NET Framework 3.5 installed. You can clone this repository and include the `ADX` namespace in your project.

```bash
git clone https://github.com/yourusername/adx-wav-converter.git
```
## Usage

### Converting ADX to WAV

```csharp
using System;
using System.IO;
using ADX;

class Program
{
    static void Main()
    {
        string adxFilePath = "path/to/yourfile.adx";
        string wavFilePath = "path/to/yourfile.wav";

        byte[] adxData = File.ReadAllBytes(adxFilePath);
        byte[] wavData = ADX.ToWav(adxData);

        File.WriteAllBytes(wavFilePath, wavData);
    }
}
```
### Converting WAV to ADX
```csharp
using System;
using System.IO;
using ADX;

class Program
{
    static void Main()
    {
        string wavFilePath = "path/to/yourfile.wav";
        string adxFilePath = "path/to/yourfile.adx";

        byte[] wavData = File.ReadAllBytes(wavFilePath);
        byte[] adxData = ADX.FromWav(wavData, loop: true);

        File.WriteAllBytes(adxFilePath, adxData);
    }
}
```
### Code Structure

- ADX.cs: The main file containing the conversion logic for ADX to WAV and WAV to ADX.
- Utils.cs: Contains utility functions for reading and writing data in big-endian format, and for handling byte and short array conversions.

### Acknowledgements

- Original ADX decoding logic by BERO: [Website](http://www.geocities.co.jp/Playtown/2004/)
- ADX format information from Yatsushi: [Website](http://ku-www.ss.titech.ac.jp/~yatsushi/adx.html)
- ADX format information from Wikipedia: [ADX](https://en.wikipedia.org/wiki/ADX_(file_format))
- WAV format information from Wikipedia: [WAV](https://en.wikipedia.org/wiki/WAV)
 
### License
This project is licensed under the MIT License. See the  [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) file for details.

Feel free to contribute to this project by submitting issues or pull requests.
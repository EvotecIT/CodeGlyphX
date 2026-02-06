---
title: Data Matrix - CodeGlyphX
description: Data Matrix encoding basics and use cases.
slug: datamatrix
collection: docs
layout: docs
---

{{< edit-link >}}

# Data Matrix

Data Matrix is a 2D barcode widely used in industrial and commercial applications for marking small items.

## Basic Usage

```csharp
using CodeGlyphX;

// Simple Data Matrix
DataMatrixCode.Save("SERIAL-12345", "datamatrix.png");

// With specific size
DataMatrixCode.Save("SERIAL-12345", "datamatrix.svg", size: DataMatrixSize.Square24);
```

## Use Cases

- **Electronics manufacturing** - Component marking and tracking
- **Healthcare** - Medical device identification (UDI)
- **Aerospace** - Part serialization
- **Postal services** - High-density mail sorting

## Features

CodeGlyphX supports all standard Data Matrix sizes from 10x10 to 144x144 modules, including rectangular variants.

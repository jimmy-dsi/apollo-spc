# The 7sb Format
## Overview
When loading Script700 files into the player, you may notice a file named `<your-spc-file>.7sb` or `65816.7sb` generated upon load.

This is a binary file which stores the Script700 bytecode, as well as the data area bytes and label address pointers.

At this moment, **apollo-spc-program** does not understand .700 or .7se files natively, but it *can* read data from .7sb files. Reading of .700 or .7se files can only be done if the player is launched via **play**, as that app is the one with the capability to parse Script700 reliably and convert it into bytecode.

## Terminology

Throughout the rest of this doc, the term "word" will refer to a 32-bit unsigned integer.

## Specification

The 7sb format is divided into three sections: The label section, the code section, and the data section.

### The Label Section
Starting from the beginning of the file, the label section consists of 4096 bytes (1024 entries, one word each). Each word refers to the relative address within the code or data section:

```
0                    4                    8      4092                4096
+--------------------+--------------------+-     -+--------------------+
|  Label 0 address   |  Label 1 address   |  ...  | Label 1023 address |
+--------------------+--------------------+-     -+--------------------+
```

Each word is stored in little endian, and the value of each word corresponds to the address within the code or data section relative to the beginning of the code/data section.

For code area addresses: The value is *word*-indexed.
For data area addresses: The value is *byte*-indexed, and the MSB of the address is set to 1.

Example:
```
; Script700 source file
; Begin Script area
nop
:0
nop
e
:: ; End script area

e
:: ; End extended area

; Begin Data area
00
:1
01
```

Each instruction takes either 1 or 2 words. In the case of `nop`, it is a single word.
This means that label 0 (`:0`) is located one word into the code section, and label 1 (`:1`) is located one *byte* into the data section. Therefore, the beginning of the label section in the 7sb file would look as follows:

```
0            4            8      
+------------+------------+-     
|  00000001  |  80000001  |  ... 
+------------+------------+-     
```

Or in its little-endian byte form:
```
0    1    2    3      4    5    6    7
+----+----+----+----+ +----+----+----+----+
| 01 | 00 | 00 | 00 | | 01 | 00 | 00 | 80 | ...
+----+----+----+----+ +----+----+----+----+
```

### The code section

The code section immediately follows the label section of the 7sb flie. It starts with a 32-bit integer (little endian) indicating the size of the bytecode *in words*, followed by the actual bytecode itself (More info on the bytecode format can be found in the [script700-bytecode](./script700-bytecode.txt) file in the doc section)

(Note: Each instruction word is stored as little endian)

### The data section

Right after the end of the code section is the data section. Similar to the code section, this begins with a 32-bit integer (little endian) indicating the size of the data. Unlike the code section however, this value indicates the number of *bytes* which follow—not words.
The size indicator is followed by the actual byte data written in the data section.
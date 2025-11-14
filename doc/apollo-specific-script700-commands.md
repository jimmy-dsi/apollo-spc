# Apollo-Specific Script700 Commands
Along with all of the original Script700 commands mentioned on [dgrfactory's website](https://dgrfactory.jp/spcplay/script700.html), Apollo adds a few extra Script700 commands of its own.

These are briefly touched upon in the [Script700 bytecode](./script700-bytecode.txt) section, but I feel it deserves its own page for it as well.

## Interrupt Commands
This section will cover all of the Script700 commands pertaining to interrupts.

### Background
One feature unique to apollo is that it supports the use of interrupts—a feature originally intended for the SPC700 that ultimately was not used when the SNES was released. (The interrupt pin *does* exist, but it was never connected to any device which could trigger it).

In theory, this means that if any device *were* to trigger an interrupt, the SPC700 should already have the capacity to handle it.

While there is no way to trigger the interrupt from an SPC file itself, one of apollo's goals is to have this feature available via scripting and debugging.

### Send interrupt signal command (i)

This sends the interrupt signal to the SPC700 using the current configured vector (by default, this is the same as the break vector).

If the interrupt is successful, a value of 1 will be stored in `[CMP2]` after this instruction.

If unsuccessful (due to either interrupts being disabled or if SPC700 is in the STOP state),
then a value of 0 will be stored in `[CMP2]`.

Usage:
```
i
```

### Reset interrupt vector address (ib)

This command will reset the interrupt location to the same as the SPC700 break vector address.

Usage:
```
ib
```

### Send interrupt and wait (iw)

Sends the interrupt signal to the SPC700 using the current configured vector, and waits until
the SPC700 writes to the specified output port number.

If the interrupt is rejected, then no wait will occur and the script will instantly resume.

If the interrupt is successful, a value of 1 will be stored in `[CMP2]` after this instruction.

If unsuccessful, then a value of 0 will be stored in `[CMP2]`.
The number of clocks waited (2,048 kHz) is stored in `[CMP1]`.

Usage:
```
iw [PORT]
```

Where `[PORT]` is any parameter from [parameter group 3](https://dgrfactory.jp/spcplay/script700.html#script_param3)—must evaluate to a number between 0 and 3 (inclusive).

### Set interrupt vector address (iv)

Configures the address to use for the interrupt vector for all subsequent `i` or `iw` commands (default is the break vector).

Usage:
```
iv [RAM]
```

Where `[RAM]` is any parameter from [parameter group 3](https://dgrfactory.jp/spcplay/script700.html#script_param3)—must evaluate to a valid 16-bit ARAM address (0x0000 to 0xFFFF inclusive)

## Other Apollo commands

Additionally, apollo defines a couple of extra commands not directly related to the interrupt feature.

### Swap CMP1 and CMP2 command (sw)

Swaps the values stored in `[CMP1]` and `[CMP2]`. The motivation for this command is that the original Script700 has no way to store the current `[CMP2]` value into work memory (or anywhere else for that matter).

Because there are a few interrupt commands which store its result in `[CMP2]`, there needs to be a reliable way to retrieve this value.

Usage:
```
sw
```

Example scenario:
```
iw 0    ; Send interrupt signal and wait for the SPC700 to write to APU IO port 0.
m #? w0 ; Number of clocks waited stored in [CMP1]. Save to w0.
sw      ; Swap [CMP1] and [CMP2]
m #? w1 ; Interrupt success flag store in [CMP2] (Now [CMP1] because of the swap). Save to w1.
```
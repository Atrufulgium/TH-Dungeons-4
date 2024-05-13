`TH-Dungeons-4/BulletScriptCompiler`
====================================
This is the compiler subproject. The compiler is a standalone DLL that simply converts code into bytecode as specified below. This project does not include a VM implementation.

&nbsp;

Awkwardly placed todo-list:
- This file. Maybe also for *once* include project setup/build instructions.
- Indices are not yet implemented in the compiler.
- The `matrix` shorthand is broken.
- Multiplication `scalar * matrix` is not supported.
- Moving things like `autoclear`, `clearimmune`, `clearingtype`, `harmsenemies`, `harmsplayers` to BSS seems more sensible than having it in code.
- More event functions, such as `on_left_side()`, `on_border()`, etc, mixed with more intrinsics `bounce(matrix2x1 normal)`, `wraparound()`, etc.

BulletScript (BS)
=================
The least original name used by at least a dozen other people! And by me, for the third time.

A typical bulletscript file looks as follows.
```js
float difficulty = 3;

function void main(float value) {
    repeat {
        bullettype = "main";
        float angle = angletoplayer();
        repeat (10) {
            repeat (difficulty) {
                spawnspeed = rng(1, 1.2);
                spawnrotation = angle + rng(-0.1, 0.1);
            }
            wait(0.1);
        }

        bullettype = "alt";
        for (angle = 0; angle < 1; angle += 0.01) {
            spawnspeed = 1;
            spawnrotation = angle;
        }
        wait(2);
    }
}

function void on_health<0.66>() { increase_difficulty(); }
function void on_health<0.33>() { increase_difficulty(); }

function void increase_difficulty() { difficulty++; }
```

This is a pretty lame pattern that shotguns over a second and ends with a simple ring of bullets, rinse and repeat over every three seconds. When health reaches 2/3 and 1/3, it gets denser.

Basics
------

Control flow
------------

Matrices
--------

Arithmetic
----------

Functions
---------

Built-in functions
------------------

Bullet variables
----------------

BulletStyleSheets (BSS (BS for short))
======================================

Special selectors
-----------------

Bullet types
------------

VM specification
================

Memory layout
-------------
The VM consists of an instructions array `float4[]`, variable memory `float[]` and string memory `string[]`. There is also an instruction pointer `int`.

Variables can either be seen as a `float`, or as a `float4`. In the latter case, it will be 4-aligned. The memory layout is quite specific.

Index   | Init    | Description
------: | :----   | :----------
0~7     | 0...0   | Reserved.
8       | 1       | Represents `autoclear`.
9       | 0       | Represents `bullettype`. Entry `0` of the string array is always `"error"`.
10      | 0       | Represents `clearimmune`.
11      | 0       | Represents `clearingtype`.
12      | 0       | Represents `harmsenemies`.
13      | 1       | Represents `harmsplayers`.
14      | 0       | Represents `spawnrotation`.
15      | 1       | Represents `spawnspeed`.
16      | 1       | Represents `spawnrelative`.
17      | 0       | Represents `usepivot`.
20~23   | `[0:0]` | Represents `spawnposition`. (Two entries unused.)
24~31   | 0...0   | Reserved.
32~*N*  | 0...0   | User variables, for some *N* the VM knows about.
*N*~*M* | 0...0   | Control flow variable block.†

† Whenever non-`main()` execution is started, the `main()` thread's block gets copied somewhere, this thread's block gets reset to zero, and then execution starts. Once done, the `main()` thread's block gets copied back. This contains variables such as "we entered this method from this label", "this method has these arguments", etc. In particular, this block contains variables such as `main()`'s float argument, which the VM can set freely.

String memory is a whole lot simpler.
Index | Init | Description
----: | :--- | :----------
0     | `"error"` | The string representing something going wrong.
1~... | (user value) | User strings. These are always initialized, and contains no duplicates.

The VM also stores the instruction pointer targets for all special methods.

Execution
---------
Standard bytecode fare (see the tables below). Note that as recursion is not supported and indexing is severely limited, we know ahead of time how large the VM's memory has to be, and we can perform checks to ensure nothing goes out of bounds. This also allows us to support switching between (waiting for) `main()` and running event functions such as `on_message()`; see the footnote below the variable memory layout table.

I don't know whether I'm going to be doing any netcode shenanigans, but the check whether all ops point to a valid location in memory is *required*.

A single execution may run at most 1 million instructions. After that, it's forcefully cut off, and continued the next execution as if a `wait(0);` was temporarily inserted.

Opcode tables
-------------

General operations.

Non-math intrinsics.

Floating point math.

Vector math, excluding matrix multiplication.

Matrix multiplication.
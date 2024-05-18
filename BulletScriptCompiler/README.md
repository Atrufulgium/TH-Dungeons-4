`TH-Dungeons-4/BulletScriptCompiler`
====================================
This is the compiler subproject. The compiler is a standalone DLL that simply converts code into bytecode as specified below. This project does not include a VM implementation.

(This part of the repo is separate from the rest in case I want to do stuff like "create an editor" or "create server-side compilation", which is wholly independent of the Unity side of things.)

&nbsp;

Awkwardly placed todo-list:
- This file. Maybe also for *once* include project setup/build instructions.
- Indices are not yet implemented in the compiler.
- Square is not yet used in the compiler.
- The entire delta of the stuff in the commit that added this line.
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
BulletScript has three variable types: `string`s, `float`s, and any `matrix` from size 1x1 to 4x4 that can also be written as `matrixRxC`.

Strings are constant `"words"` that cannot be modified at runtime. The only thing you can do with these, is copying them over.

Floats are more interesting. These are numbers such as `1`, `3.14`, or `1e9`. Like c#, you may write an `f` after the numbers, but this is not required. Booleans `true` and `false` are also just floats, represented as `1` (or "non-zero" in any check) and `0`.

Matrices are blocks of numbers of size "rows × columns" as in math. They range between `1x1` and `4x4` in size. Vectors (of the form `Rx1` or `1xC`) can be seen as both standing and lying depending on what is needed for well-formed operations.

Most matrices are written as follows: `[1 2 3 4; 5 6 7 8]` represents the matrix $\bigl(\begin{smallmatrix}1 & 2 & 3 & 4 \\ 5 & 6 & 7 & 8\end{smallmatrix}\bigr)$. Two-vectors also have a polar form `[angle:radius]` available.

Contrary to every standard in existence, `angle` **is a [0,1] value representing the (clockwise) unit "turn"**. An angle that is `0` turn points straight down, while a turn of `0.25` points to the left. For instance, `[5/8 : sqrt(2)]` is (approximately) the same as `[1; 1]`.

Matrices can be indexed by `[index]` to get (in row-major 0-indexed order) an entry of that matrix. It can also be accessed as `[row; col]`.

Code can be written in two forms. Either a list of statements:
```js
a = 1;
b = 2;
repeat (c) {
    break;
}
```
or a list of variable and method declarations:
```js
float a = 1;

function void main(float b) {
    float b = 2;
    repeat (c) {
        break;
    }
}
```

Methods may not be defined inside other methods. There is very basic scope: if the inner function defines an `a` as well, the outer `a` will not be accessed.


Control flow
------------
Control flow is very similar to c#. `if`, `else`, `for`, and `while` are the same, except that the subsequent statement must always be a block of code, and not a single line.

In addition, there are two variants of a _repeat_ loop:

```js
repeat {
    // This code runs eternally.
}
repeat (a) {
    // This code rusn `a` times.
}
```

All loop types can use `break;` and `continue;`.

Arithmetic
----------

Any variable can be assigned to another compatible variable with `=`. Furthermore, floats and matrices have the following operations: `-a`, `!a` (boolean NOT), `a^b` (exponentiation), `a*b`, `a/b`, `a%b`, `a+b`, `a-b`, `a<b`, `a>b`, `a<=b`, `a>=b`, `a==b`, `a!=b`, `a&b` (boolean AND), `a|b` (boolean OR). For matrices, all of these are applied entry wise if the sizes match.

However, matrices have one exception: `a*b`, when matrices are of compatible size, is matrix multiplication instead. In particular, if `a` and `b` are same-sized, matrix multiplication is applied.

All binary operators, except for the comparators, can be used in compound assignments as for instance `a += b`.

Functions
---------
A function definition looks as follows:
```js
function type name(argtype1 arg1, .., argtypeN argN) {
    ..
}
```
with any or no arguments. The return type may be any valid type, but also `void`. Matrix sizes need to be explicit in the signature. Just `matrix` is not allowed, you must use `matrixRxC`.

Recursion is not supported, and function definitions within function definitions are not allowed.

There are some methods that do something special when you define them. None of these are required.
- `function void main(float value)` is called when the script is loaded and the primary entry points. A `value` is optionally sent from the parent script (where "parent" means the script that created this script with `addscript()`, `startscript()`, or `startscriptmany()`). Otherwise, it is constant 0.
- `function void on_message(float value)` is called whenever the _any_ script associated with the same enemy calls `message()`. The argument of the message is passed.
- `function void on_charge(float value)` is similar to `message()`, but when calling `charge()`, and with a delay.
- `function void on_health<fraction>()` is called whenever the attached mob's health reaches a certain fraction of its starting health (in `[0,1]`). Of particular note is `function void on_health<0>()` that is guaranteed to run when the character is killed. This `fraction` must be a compile-time constant.
- `function void on_time<seconds>()` is called after a certain amount of time has passed (since being instantiated by the parent).
- `function void on_screen_leave(float value)` is called whenever the bullet has visibly left the screen. The `value` represents where the bullet went off-screen: `0` means "down", `1` means "left", `2` means "up", and `3` means "right". This method can be used to created bounces, bullets, wraparounds, etc.

All these special `on_X` methods may not call `wait()`, whether directly or indirectly.

Built-in functions
------------------

These are all functions that communicate with the c# side of things. You can call these like a normal user-defined function. In the table below, `any` means that any type is allowed. Matrix-related functions must have properly sized arguments, and _usually_ return something of the same size. The `Many?` column denotes whether a function gets called for every attached object, or just once.

The methods with both a float and matrix formulation _usually_ apply the float formula entry-wise to the matrix. As such, the matrix sizes should match.

Signature | Many? | Notes
----------|-------|------
`void print(any)` | ❌ | Prints a string or value to the log.
`void wait(float)` | ❌ | Stops execution for a specified amount of ticks. Only allowed in `main()`.
`void message(float)` | ❌ | Triggers all `on_message(float)` methods in all scripts connected to this enemy. 
`void charge(float)` | ❌ | Does the "fwooOO" charge particle- and sound effect around the connected enemy. Once this charge is finished (taking approximately 2 seconds), `on_charge(float)` is called in all scripts connected to this enemy.
`void spawn()` | ✅ | Creates a bullet from the current settings.
`void destroy()` | ✅ | Destroys all affected objects and stops the script from executing immediately.
`void loadbackground(string)` | ❌ | Load a background scene. This is asynchronous, so there may be a little delay.
`void addscript([string [, float]])` | ❌ | Adds a script to all bullets spawned in this tick since last time this was called. This list is reset by calling the version with no argument, or with `""`. Can pass an optional value to the script. If none is provided, the provided value will be an integer describing the how-many'th bullet this is on which this script is attached.
`void startscript(string[, float])` | kept | Starts a script with the same list of affected objects. Can pass an optional value.
`void startscriptmany(string[, float])` | ✅ | For each affected object, starts a script. Can pass an optional value.
`void depivot()` | ✅ | Unpivots any pivoted affected objects. This maintains their current position, rotation, and velocity.
`void rotate(float)` | ✅ | Rotates all affected objects in turns.
`void setrotation(float)` | ✅ | Sets the rotation of all affected objects in turns.
`void faceplayer()` | ✅ | Make all affected objects rotate towards the player.
`void bounce(float)` | ✅ | Make all affected bullets "bounce" into the direction of a turn.<br/>This bouncing behaviour means the following. Suppose the argument is `0.25`, meaning "bounce towards the left". Then bullets moving left in any capacity are unchanged, but bullets moving right in any capacity are mirrored around a normal.<br/>Concretely, in this example, `←` and `↖` etc stay the same, `→` turns into `←`, `↘` turns into `↙`, etc.<br/>Double-sided bouncing can be achieved by calling `bounce(a); bounce(a + 0.5);`.
`void addspeed(float)` | ✅ | Add speed¹ to all affected objects.
`void setspeed(float)` | ✅ | Sets the speed¹ of all affected objects.
`matrix2x1 playerpos()` | ❌ | Returns the player's position on the screen².
`float angletoplayer()` | ❌ | Returns the angle from an arbitrary affected object to the player in turns.
`float random(float, float)`<br/>`matrix random(matrix, matrix)` | ❌ | Returns a random number between the two endpoints, inclusive.
`void gimmick(string[, float[, float]])` | ❌ | Runs a named gimmick. Think of Seija's flipping or Sakuya's timestop or something.
`void playsound(string)` | ❌ | Plays a sound for each affected object.
`float sin(float)`<br/>`matrix sin(matrix)` | ❌ | Returns the sine, in radians.
`float cos(float)`<br/>`matrix cos(matrix)` | ❌ | Returns the cosine, in radians.
`float tan(float)`<br/>`matrix tan(matrix)` | ❌ | Returns the tangent, in radians.
`float asin(float)`<br/>`matrix asin(matrix)` | ❌ | Returns the inverse sine, in radians. The argument must be in [-1,1].
`float acos(float)`<br/>`matrix acos(matrix)` | ❌ | Returns the inverse cosine, in radians. The argument must be in [-1,1].
`float atan(float)`<br/>`matrix atan(matrix)` | ❌ | Returns the inverse tangent, in radians. You should prefer `atan2`.
`float atan2(float, float)`<br/>`matrix atan2(matrix, matrix)` | ❌ | Returns the inverse tangent of `2nd arg/1st arg`, in radians. This matches `atan` in the positive quadrant. Intuitively, the angle of the line between (0,0) and (`1st arg`, `2nd arg`).
`float angle2rad(float)`<br/>`matrix angle2rad(matrix)` | ❌ | Converts the [0,1] counterclockwise angle to radians.
`float rad2angle(float)`<br/>`matrix rad2angle(matrix)` | ❌ | Converts an angle in radians to the angle the bullet stuff uses.
`float ceil(float)`<br/>`matrix ceil(matrix)` | ❌ | Returns the smallest integer larger than the argument.
`float floor(float)`<br/>`matrix floor(matrix)` | ❌ | Returns the largest integer smaller than the argument.
`float round(float)`<br/>`matrix round(matrix)` | ❌ | Returns the integer nearest to the argument.
`float abs(float)`<br/>`matrix abs(matrix)` | ❌ | Removes the minus sign.
`float length(float)`<br/>`float length(matrix)` | ❌ | Calculates the Euclidean norm of a matrix.
`float distance(matrix, matrix)` | ❌ | Calculates the Euclidean distance between two same-sized matrices.
`float mcols(matrix)` | ❌ | The number of columns in a matrix.
`float mrows(matrix)` | ❌ | The number of rows in a matrix.

¹ Speed is measured in "units per second".  
² The screen has coordinates `[0; 0]` on the top-left, and `[6/5; 1]` on the bottom-right.

Bullet variables
----------------
There are some predefined variables that are referenced when creating bullets (with `spawn()`). These are:

Name | Type | Default | Description
-----|------|---------|-------------
`spawnspeed` | `float` | `1` | The speed¹ of the bullet.
`spawnposition` | `matrix` | `[0;0]` | Where on the screen² the bullet will be spawned.
`spawnrotation` | `float` | `0` | A number (in turns) describing what direction the bullet will move when spawned.
`spawntype` | `float` | `0` | Whether this bullet is spawned relative to the current script executor:<br/>・ `0` (_Relative_) Both position and rotation are relative to that of the parent.<br/>・ `1` (_Relative position_) Only the position is relative, the rotation is absolute.<br/>・ `2` (_Relative rotation_) Only the rotation is relative, the position is absolute.<br/>・ `3` (_Absolute_) Both position and rotation are absolute.
`bullettype` | `string` | `"error"` | What bullet stylesheet to grab. Is allowed to reference multiple sheets separated by spaces; later sheets combine with the properties of earlier sheets, except when a property is marked `!important`. If any sheet fails to load, falls back to the `error` sheet.

¹ Speed is measured in "units per second".  
² The screen has coordinates `[0; 0]` on the top-left, and `[6/5; 1]` on the bottom-right.

BulletStyleSheets (BSS (BS for short))
======================================
Each enemy has a collection of css-like _bullet style sheets_. These determine the type and general behaviour of the bullet. Scripts can reference these stylesheets when creating bullets.

Overlay rules work as in css. Later sheets overwrite previous sheets' content, except when marked with `!important`. Specificity rules apply. Unlike css, as "order" is ill-defined, when multiple selectors have the _same_ specificity, the result is arbitrary. Some simple examples.
```css
.a {
    bullet-type: ball;
    outer-color: #ff0000;
}
.b {
    outer-color: #00ff00;
}
.b2 {
    outer-color: #00ff00 !important;
}
.b:nth-spawn(3), .b2:nth-spawn(3) {
    outer-color: #0000ff;
}
```
In this case, `"a"` would give a red ball, as would `"b a"`. Meanwhile, `"a b"`, `"a b2"`, and `"b2 a"` all behave the same: each three `spawn()`s would give two green balls, and one blue ball.

Also unlike css, there is no "hierarchy" to speak of. When targeting multiple classes at once, simply space-separate them, and when listing multiple specifiers, separate them with comma's.

A full list of properties is as follows.

Property | Default | Description
---------|---------|-------------
`bullet-type` | n/a | What this bullet looks like. See the below chart for details. Each bullet comes with its own default stylesheet.
`outer-color` | n/a | The color that is applied to usually the outside of a bullet. This corresponds to the `G` channel in the bullet texture.
`inner-color` | `#ffffff` | The color that is applied to usually the inside of a bullet. This corresponds to the `R` channel in the bullet texture.
`filter-color` | `#ffffff` | Both `inner-color` and `outer-color` are multiplied by this factor.
`cleared` | `regular` | When this bullet gets destroyed. One of:<br/>・ `never`: The bullet only disappears between spell-cards or when `destroy()`ed from script.<br/>・ `edge-only`: In addition to the above, the bullet disappears when too far beyond the field's edge.<br/>・ `regular`: In addition to the above, the bullet disappears by bombs, when dying, or when colliding with the correct _clearing_ bullets.<br/>・ `own-clear`: In addition to the above, when this is a clearing bullet, it disappears when it causes another bullet to disappear.
`clearing-type` | `none` | What bullets this bullet clears. One of:<br/>・ `none`: This is not a clearing bullet.<br/>・ `other`: This clears only bullets of the opponent.<br/>・ `self`: This clears everything but bullets of the opponent.<br/>・ `everything`: This clears other bullet.
`harms` | `players` | What this bullet harms. One of:<br/>・ `players`: Harms only players.<br/>・ `enemies`: Harms only enemies.<br/>・ `harmless`: Harms no one.
`can-spawn-on-player` | `false` | Whether this bullet can spawn extremely close or on top of the player.
`hitbox-scale`¹ | `1` | What factor this bullet's hitbox gets scaled to in addition to `scale`.
`hitbox-scale-Y`¹ | `1` | What factor this bullet's hitbox get scaled vertically in addition to `hitbox-scale`.
`render-mode` | `regular` | The transparency blend mode. One of:<br/>・ `regular`: Regular transparency blending.<br/>・ `add`: Additive blending. This makes bullets "glowy".
`rotation` | `regular` | How bullets visually rotate. One of:<br/>・ `regular`: Bullets rotate to indicate their movement.<br/>・ `fixed`: Bullets always have a rotation of 0.<br/>・ `continuous`: Bullets rotate constantly.<br/>・ `wiggle`: Bullets have a rotation that oscillates slightly around 0.<br/>This property is compatible with `hitbox-scale-Y`.
`scale`¹ | `1` | What factor this bullet AND its hitbox get scaled to.
`layer` | `0` | What layer this bullet gets rendered on wrt the other bullets. Higher values get drawn on top.
`snake-length` | `0` | If present, described how long this snake get spawned for, in seconds. Mutually exclusive with `laser-length`.
`laser-length` | `0` | If present, describes how long this laser lasts, in seconds. Mutually exclusive with `snake-length`.
`laser-charge` | `0` | If `laser-length` is present, describes how many seconds of its duration is spent as a harmless warning.
`is-pivot` | `false` | Whether this bullet can be pivoted to, meaning that each pivoted bullet's position and movement is entirely relative to this bullet's.
`pivoted` | `false` | Whether to pivot to the last created bullet with `is-pivot` set to `true` *this tick*.

¹ When multiple sheets are applied, these properties do not overwrite, but instead multiply.

Special selectors
-----------------
Apart from the "class-like" notation that can be used inside scripts, there are a few more specifiers.

The first kind is "tag-like" and denotes a list of characters. For instance,
```css
reiuji_utsuho {
    bullet-type: sun !important;
}
```
would replace all of Okuu's bullets with suns, and have no effect on other characters. Seems fair.

There are also "pseudo-class likes". An exhaustive list is the following:

Pseudo-class | Description
-------------|-------------
`:first-each-tick` | Matches with the first bullet *that a specific script* (not the character!) `spawn()`s in a tick.
`:grazed` | Matches bullets that have been grazed by the player at least once.
`:grazing` | Matches bullets that are currently being grazed by the player.
`:last-each-tick` | Matches with the last bullet *that a specific script* (not the character!) `spawn()`s in a tick.
`:killed` | Matches bullets that have killed the player.
`:nth-message(N)` | Matches bullets whose attached scripts have run a total of `N` `on_message()` calls.
`:nth-spawn(N)` | Matches with each `N`th bullet of a type *that a specific script* (not the character!) `spawn()`s.

Bullet types
------------
(The chart's _still_ in the other repo.)

VM specification
================

Memory layout
-------------
The VM consists of an instructions array `float4[]`, variable memory `float[]` and string memory `string[]`. There is also an instruction pointer `int`.

Variables can either be seen as a `float`, or as a `float4`. In the latter case, it will be 4-aligned. The memory layout is quite specific.

Index   | Init    | Description
------: | :----   | :----------
0       | 0       | Represents `bullettype`. Entry `0` of the string array is always `"error"`.
1       | 0       | Represents `spawnrotation`.
2       | 1       | Represents `spawnspeed`.
3       | 1       | Represents `spawnrelative`.
4~8     | `[0:0]` | Represents `spawnposition`. (Two entries unused.)
9       | 0       | Represents `main(float)`'s argument.
10      | 0       | Represents `on_message(float)`'s argument.
11      | 0       | Represents `on_charge(float)`'s argument.
12      | 0       | Represents `on_screen_leave(float)`'s argument.
13~*N-1*| 0...0   | User variables, for some *N* the VM knows about.
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
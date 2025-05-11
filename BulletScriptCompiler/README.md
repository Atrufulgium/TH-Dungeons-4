`TH-Dungeons-4/BulletScriptCompiler`
====================================
This is the compiler subproject. The compiler is a standalone DLL that simply converts code into bytecode as specified below. This project does not include a VM implementation.

(This part of the repo is separate from the rest in case I want to do stuff like "create an editor" or "create server-side compilation", which is wholly independent of the Unity side of things.)

### Awkwardly placed todo-list:
Compiler:
- Writing to indices `a[i] = ...` is not implemented at any level yet.
- Square is not yet used in the compiler.
- All of those event functions `on_..<>`.
- More event functions, such as `on_left_side()`, `on_border()`, etc, mixed with more intrinsics `bounce(matrix2x1 normal)`, `wraparound()`, etc.
- The `matrix` shorthand is broken.
- Multiplication `scalar * matrix` is not supported. (We could do this as `MatrixMul11N` and `MatrixMulN11` opcodes per row.)
- Increments/decrements are currently only really allowed at statement-level.

Other:
- This file. Maybe also for *once* include project setup/build instructions.
- Even though the keys behave properly, glowy/nonglowy render passes are ordered arbitrarily.
- Everything BSS. This includes removing the variables `autoclear`, `clearimmune`, `clearingtype`, `harmsenemies`, `harmsplayers`, `use_pivot` from the compiler.

BulletScript (BS)
=================
The least original name used by at least a dozen other people! And by me, for the third time.

A typical BulletScript file looks as follows.
```js
float difficulty = 3;

// Main entrypoint when the script is spawned in.
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

// Triggers when the boss has 2/3 health left.
function void on_health<0.66>() { increase_difficulty(); }
// Triggers when the boss has 1/3 health left.
function void on_health<0.33>() { increase_difficulty(); }

// Custom function definition
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

However, matrices have one exception: `a*b`, when matrices are of compatible size, is matrix multiplication instead. In particular, if `a` and `b` are same-sized square matrices, matrix multiplication is applied.

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
- `function void on_health<fraction>()` is called whenever the attached mob's health reaches a certain fraction of its starting health (in `[0,1]`). Of particular note is `function void on_health<0>()` that is guaranteed to run when the character is killed. This `fraction` must be a simple float literal.
- `function void on_time<seconds>()` is called after a certain amount of time has passed (since being instantiated by the parent).
- `function void on_screen_leave(float value)` is called whenever the bullet has visibly left the screen. The `value` represents where the bullet went off-screen: `0` means "down", `1` means "left", `2` means "up", and `3` means "right". This method can be used to created bounces, bullets, wraparounds, etc.

All these special `on_X` methods may not call `wait()`, whether directly or indirectly.

Built-in functions
------------------

These are all functions that communicate with the c# side of things. You can call these like a normal user-defined function. The `Many?` column denotes whether a function gets called for every attached object, or just once.

The methods with both a float and vetor formulation _usually_ apply the float formula entry-wise to the vector. As such, the vector sizes should match.

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
`float turnstoplayer()` | ❌ | Returns the angle from an arbitrary affected object to the player in turns.
`float random(float, float)`<br/>`vector random(vector, vector)` | ❌ | Returns a random number between the two arguments, inclusive.
`void gimmick(string[, float[, float]])` | ❌ | Runs a named gimmick. Think of Seija's screen-flipping or Sakuya's timestop or something.
`void playsound(string)` | ❌ | Plays a sound for each affected object.
`float sin(float)`<br/>`vector sin(vector)` | ❌ | Returns the sine, in radians.
`float cos(float)`<br/>`vector cos(vector)` | ❌ | Returns the cosine, in radians.
`float tan(float)`<br/>`vector tan(vector)` | ❌ | Returns the tangent, in radians.
`float asin(float)`<br/>`vector asin(vector)` | ❌ | Returns the inverse sine, in radians. The argument must be in [-1,1].
`float acos(float)`<br/>`vector acos(vector)` | ❌ | Returns the inverse cosine, in radians. The argument must be in [-1,1].
`float atan(float)`<br/>`vector atan(vector)` | ❌ | Returns the inverse tangent, in radians. You should prefer `atan2`.
`float atan2(float, float)`<br/>`vector atan2(vector, vector)` | ❌ | Returns the inverse tangent of `2nd arg/1st arg`, in radians. This matches `atan` in the positive quadrant. Intuitively, the angle of the line between (0,0) and (`1st arg`, `2nd arg`).
`float turn2rad(float)`<br/>`vector turn2rad(vector)` | ❌ | Converts the [0,1] clockwise turn to radians.<br/>In other words, 0, 0.25, 0.5, and 0.75 turn map to $\frac32\pi$, $\pi$, $\frac12\pi$, and 0 radians, respectively.<br/>This is equivalent to `(1.75f - float) * TAU % TAU`.
`float rad2turn(float)`<br/>`vector rad2turn(vector)` | ❌ | Converts an angle in radians to the angle the bullet stuff uses.<br/>This is equivalent to `(1.75 - float / TAU) % 1`.
`float ceil(float)`<br/>`vector ceil(vector)` | ❌ | Returns the smallest integer larger than the argument.
`float floor(float)`<br/>`vector floor(vector)` | ❌ | Returns the largest integer smaller than the argument.
`float round(float)`<br/>`vector round(vector)` | ❌ | Returns the integer nearest to the argument.
`float abs(float)`<br/>`vector abs(vector)` | ❌ | Removes the minus sign.
`float length(float)`<br/>`float length(vector)` | ❌ | Calculates the Euclidean norm of a vector.
`float distance(vector, vector)` | ❌ | Calculates the Euclidean distance between two same-sized vectors.
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

BulletStyleSheets (BSS for short (BS for short))
================================================
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
`:after-time(N)` | Matches bullets that have existed for at least `N` ticks.
`:before-time(N)` | Matches bullets that have existed for at most `N` ticks.

Bullet types
------------
(The chart's _still_ in the other repo.)

VM specification
================

Memory layout
-------------
The VM consists of an instructions array `float4[]`, variable memory `float[]` and string memory `string[]`. There is also an instruction pointer `int`. The instructions array may contain at most `0xffff` entries.

Variables can either be seen as a `float`, or as a `float4`. In the latter case, the variable shall be 4-aligned. The memory layout is quite specific.

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
Standard bytecode fare (see the tables below). Note that as recursion is not supported and indexing is severely limited, we know ahead of time how large the VM's memory has to be, and we can perform checks to ensure nothing goes out of bounds. This also allows us to support switching between (waiting for) `main()` and running event functions such as `on_message()` without actually having to support a threading model; see the footnote below the variable memory layout table.

I don't know whether I'm going to be doing any netcode shenanigans, but the check whether all ops point to a valid location in memory is *required*.

A single execution may run at most 1 million instructions. After that, it's forcefully cut off, and continued the next execution as if a `wait(0);` was temporarily inserted.

The output of the execution is a list of commands the engine will have to consume.

Opcode tables
-------------

The syntax in the below tables is consistent for easier browsing.
- Literal values don't have an asterisk (`*`), while variables do.
- The written location (if any) will be called `r`.
- Other read locations will be called `i`, `j`.
- Literal values will be called `v`, `w`.
- String variables will be called `*s`, `*t`.  
  Strings are nothing more than float variables with an interpretation.

For more details on the below opcodes, see the function documentation above.

### General operations
These operations are the most basic things you'd expect in a VM and defined as "everything that does not fit the below categories".

| Name       | OpCode | Args | Description
| :--------- | :----- | :--- | :----------
| Equal      |  1 | `*r  v *i` | Set `*r` to 1 if `v == *i`, 0 otherwise.
| Equal      |  2 | `*r *i *j` | Set `*r` to 1 if `*i == *j`, 0 otherwise.
|            |  3 | | *(Removed)*
| LT         |  4 | `*r  v *i` | Set `*r` to 1 if `v < *i`, 0 otherwise.
| LT         |  5 | `*r *i  v` | Set `*r` to 1 if `*i < v`, 0 otherwise.
| LT         |  6 | `*r *i *j` | Set `*r` to 1 if `*i < *j`, 0 otherwise.
| LTE        |  7 | `*r  v *i` | Set `*r` to 1 if `v <= *i`, 0 otherwise.
| LTE        |  8 | `*r *i  v` | Set `*r` to 1 if `*i <= v`, 0 otherwise.
| LTE        |  9 | `*r *i *j` | Set `*r` to 1 if `*i <= *j`, 0 otherwise.
| Set        | 10 | `*r  v` | Set `*r` to `v`.
| Set        | 11 | `*r *i` | Set `*r` to `*i`.
|            | 12 | | *(Removed)*
| IndexedGet | 13 | `*r *i  v` | Set `*r` to `*(i+v)`. Offset `v` will be clamped to `[0,16)`.
| IndexedGet | 14 | `*r *i *j` | Set `*r` to `*(i+*j)`. Offset `*j` will be clamped to `[0,16)`.
| IndexedSet | 15 | `*r  v  w` | Set `*(r+v)` to `w`. Offset `v` will be clamped to `[0,16)`.
| IndexedSet | 16 | `*r  v *i` | Set `*(r+v)` to `*i`. Offset `*i` will be clamped to `[0,16)`.
| IndexedSet | 17 | `*r *i  v` | Set `*(r+*i)` to `v`. Offset `v` will be clamped to `[0,16)`.
| IndexedSet | 18 | `*r *i *j` | Set `*(r+*i)` to `*j`. Offset `*j` will be clamped to `[0,16)`.
| Jump       | 19 | ` v` | Set the IP to `v`.<br/>*Note*: The IP will also increase after this instruction.
| JumpCond.  | 20 | ` v  *j` | Set the IP to `v` if `*j != 0`, do nothing otherwise.<br/>*Note*: The IP will also increase after this instruction.

### Messaging intrinsics
The goal of the VM is to create an output stream with filled-in commands for the engine to work with.

These operations generate those commands.

| Name            | OpCode | Args | Description
| :-------------- | :----- | :--- | :----------
| Message         | 32 | `*i` | Output a "message `*i` to other scripts"-message.
| Message         | 33 | ` v` | Output a "message `v` to other scripts"-message.
| Spawn           | 34 | | Output a "spawn bullets"-message with the current settings.
| Destroy         | 35 | | Output a "destroy self"-message.
| LoadBackground  | 36 | `*s` | Output a "load background `*s`"-message.
| AddScript       | 37 | | Output an "add script barrier"-message.
| AddScript       | 38 | `*s` | Output an "add script `*s`"-message.
| Addscript       | 39 | `*s  v` | Output an "add script `*s` with starting value `v`"-message.
| AddScript       | 40 | `*s *i` | Output an "add script `*s` with starting value `*i`"-message.
| StartScript     | 41 | `*s` | Output a "start script `*s`"-message.
| Startscript     | 42 | `*s  v` | Output a "start script `*s` with starting value `v`"-message.
| StartScript     | 43 | `*s *i` | Output a "start script `*s` with starting value `*i`"-message.
| StartScriptMany | 44 | `*s` | Output a "start script `*s` for each child"-message.
| StartscriptMany | 45 | `*s  v` | Output a "start script `*s` for each child with starting value `v`"-message.
| StartScriptMany | 46 | `*s *i` | Output a "start script `*s` for each child with starting value `*i`"-message.
| Depivot         | 47 | | Output a "depivot"-message.
| AddRotation     | 48 | ` v` | Output an "add `v` rotation"-message.
| AddRotation     | 49 | `*i` | Output an "add `*i` rotation"-message.
| SetRotation     | 50 | ` v` | Output a "set `v` rotation"-message.
| SetRotation     | 51 | `*i` | Output a "set `*i` rotation"-message.
| AddSpeed        | 52 | ` v` | Output an "add `v` speed"-message.
| AddSpeed        | 53 | `*i` | Output an "add `*i` speed"-message.
| SetSpeed        | 54 | ` v` | Output a "set `v` speed"-message.
| SetSpeed        | 55 | `*i` | Output a "set `*i` speed"-message.
| FacePlayer      | 56 | | Output a "face player"-message.
| AngleToPlayer   | 57 | `*r` | output a "set `*r` to the player angle"-message.
| Gimmick         | 58 | `*s` | Output a "use named gimmick `*s`"-message.
| Gimmick         | 59 | `*s  v` | Output a "use named gimmick `*s(v)`"-message.
| Gimmick         | 60 | `*s *i` | Output a "use named gimmick `*s(*i)`"-message.
| Gimmick         | 61 | `*s  v  w` | Output a "use named gimmick `*s(v,w)`"-message.
| Gimmick         | 62 | `*s  v *i` | Output a "use named gimmick `*s(v,*i)`"-message.
| Gimmick         | 63 | `*s *i  v` | Output a "use named gimmick `*s(*i,v)`"-message.
| Gimmick         | 64 | `*s *i *j` | Output a "use named gimmick `*s(*i,*j)`"-message.
| Print           | 65 | `*s` | Output a "`*s`"-message.
| Print           | 66 | ` v` | Output a "`v`"-message.
| Print           | 67 | `*r  v  w` | Output a "`*r`-message, interpreting `*r` as the start of a `v`$\times$`w` matrix.
| Wait            | 68 | ` v` | Send a "stop execution for `v` game ticks"-message.<br/>*Note*: Only allowed in `main()`.
| Wait            | 69 | `*i` | Send a "stop execution for `*i` game ticks"-message.<br/>*Note*: Only allowed in `main()`.

### Floating point math
Exactly as it says on the tin.

| Name      | OpCode | Args | Description
| :-------- | :----- | :--- | :----------
| Not       |  80 | `*r *i` | Set `*r` to `!(*i)`.
| Add       |  81 | `*r  v *i` | Set `*r` to `v + *i`.
| Add       |  82 | `*r *i *j` | Set `*r` to `*i + *j`.
| Sub       |  83 | `*r  v *i` | Set `*r` to `v - *i`.
| Sub       |  84 | `*r *i  v` | Set `*r` to `*i - v`.
| Sub       |  85 | `*r *i *j` | Set `*r` to `*i - *j`.
| Mul       |  86 | `*r  v *i` | Set `*r` to `v * *i`.
| Mul       |  87 | `*r *i *j` | Set `*r` to `*i * *j`.
| Div       |  88 | `*r  v *i` | Set `*r` to `v / *i`.
| Div       |  89 | `*r *i  v` | Set `*r` to `*i / v`.
| Div       |  90 | `*r *i *j` | Set `*r` to `*i / *j`.
| Mod       |  91 | `*r  v *i` | Set `*r` to `v % *i`.
| Mod       |  92 | `*r *i  v` | Set `*r` to `*i % v`.
| Mod       |  93 | `*r *i *j` | Set `*r` to `*i % *j`.
| Pow       |  94 | `*r  v *i` | Set `*r` to `v ^ *i`.
| Pow       |  95 | `*r *i  v` | Set `*r` to `*i ^ v`.
| Pow       |  96 | `*r *i *j` | Set `*r` to `*i ^ *j`.
| Square    |  97 | `*r *i` | Set `*r` to `*i * *i`.
| Sin       |  98 | `*r *i` | Set `*r` to `sin(*i)`.
| Cos       |  99 | `*r *i` | Set `*r` to `cos(*i)`.
| Tan       | 100 | `*r *i` | Set `*r` to `tan(*i)`.
| Asin      | 101 | `*r *i` | Set `*r` to `asin(*i)`.
| Acos      | 102 | `*r *i` | Set `*r` to `acos(*i)`.
| Atan      | 103 | `*r *i` | Set `*r` to `atan(*i)`.
| Atan2     | 104 | `*r  v *i` | Set `*r` to `atan2(v, *i)`.
| Atan2     | 105 | `*r *i  v` | Set `*r` to `atan2(*i, v)`.
| Atan2     | 106 | `*r *i *j` | Set `*r` to `atan2(*i, *j)`.
| Angle2Rad | 107 | `*r *i` | Converts an angle from turns `*i` to radians `*r`.
| Rad2Angle | 108 | `*r *i` | Converts an angle from radians `*i` to turns `*r`.
| Ceil      | 109 | `*r *i` | Set `*r` to `ceil(*i)`.
| Floor     | 110 | `*r *i` | Set `*r` to `floor(*i)`.
| Round     | 111 | `*r *i` | Set `*r` to `round(*i)`.
| Abs       | 112 | `*r *i` | Set `*r` to `\|*i\|`.
| Random    | 113 | `*r  v  w` | Set `*r` to a random float `[v,w]`.
| Random    | 114 | `*r  v *i` | Set `*r` to a random float `[v,*i]`.
| Random    | 115 | `*r *i  v` | Set `*r` to a random float `[*i,v]`.
| Random    | 116 | `*r *i *j` | Set `*r` to a random float `[*i,*j]`.
| Distance  | 117 | `*r  v *i` | Set `*r` to `\|v-*i\|`.
| Distance  | 118 | `*r *i  v` | Set `*r` to `\|*i-v\|`.
| Distance  | 119 | `*r *i *j` | Set `*r` to `\|*i-*j\|`.

### Vector math, excluding matrix multiplication
Four-aligned operations. Note that variables only point to the first entry of the vector/matrix.

Opcodes that are named `opcode4` do exactly the same as `opcode`, but entrywise on the entire four-aligned section.

There are no literal versions for these opcodes (as there is no room for them).

| Name       | OpCode | Args | Description
| :--------- | :----- | :--- | :----------
| Set4       | 128 | `*r *i` | *(〃)*
| Equal4     | 129 | `*r *i *j` | *(〃)*
| LT4        | 130 | `*r *i *j` | *(〃)*
| LTE4       | 131 | `*r *i *j` | *(〃)*
| Negate4    | 132 | `*r *i` | Set all entries of `*r` to `-(*i)`.
| Not4       | 133 | `*r *i` | *(〃)*
| Add4       | 134 | `*r *i *j` | *(〃)*
| Sub4       | 135 | `*r *i *j` | *(〃)*
| Mul4       | 136 | `*r *i *j` | *(〃)*
| Div4       | 137 | `*r *i *j` | *(〃)*
| Mod4       | 138 | `*r *i *j` | *(〃)*
| Pow4       | 139 | `*r *i *j` | *(〃)*
| Square4    | 140 | `*r *i` | *(〃)*
| Sin4       | 141 | `*r *i` | *(〃)*
| Cos4       | 142 | `*r *i` | *(〃)*
| Tan4       | 143 | `*r *i` | *(〃)*
| Asin4      | 144 | `*r *i` | *(〃)*
| Acos4      | 145 | `*r *i` | *(〃)*
| Atan4      | 146 | `*r *i` | *(〃)*
| Atan24     | 147 | `*r *i *j` | *(〃)*
| Angle2Rad4 | 148 | `*r *i` | *(〃)*
| Rad2Angle4 | 149 | `*r *i` | *(〃)*
| Ceil4      | 150 | `*r *i` | *(〃)*
| Floor4     | 151 | `*r *i` | *(〃)*
| Round4     | 152 | `*r *i` | *(〃)*
| Abs4       | 153 | `*r *i` | *(〃)*
| Random4    | 154 | `*r *i *j` | *(〃)*
| Length4    | 155 | `*r *i` | Set non-vector `*r` to `\|\|*i\|\|`.
| Distance4  | 156 | `*r *i *j` | Set non-vector `*r` to `\|\|*i-*j\|\|`.
| Polar      | 157 | `*r  v  w` | Converts a point `(v,w)` from polar to Cartesian `(*r, *(r+1))`.
| Polar      | 158 | `*r  v *i` | Converts a point `(v,*i)` from polar to Cartesian `(*r, *(r+1))`.
| Polar      | 159 | `*r *i  v` | Converts a point `(*i,v)` from polar to Cartesian `(*r, *(r+1))`.
| Polar      | 160 | `*r *i *j` | Converts a point `(*i,*j)` from polar to Cartesian `(*r, *(r+1))`.

### Matrix multiplication
Also used for scalar-vector multiplication.

Here, $u$, $v$, and $w$ go over 1 through 4 (but in the opcode silently subtract one ssssh).

| Name | OpCode | Args | Description
| :--------------- | :---------------- | :--------- | :---
| MatrixMul(u,v,w) | 192 + u +4v + 16w | `*r *i *j` | Set `*r` $\in \mathbb R^{u \times w}$ to $\mathbb R^{u \times v} \ni$ `*i` $\cdot$ `*j` $\in \mathbb R^{v \times w}$.
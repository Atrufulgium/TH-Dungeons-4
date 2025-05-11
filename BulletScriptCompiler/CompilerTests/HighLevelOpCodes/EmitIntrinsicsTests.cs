using static Atrufulgium.BulletScript.Compiler.Tests.Helpers.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atrufulgium.BulletScript.Compiler.HighLevelOpCodes.Tests {

    [TestClass]
    public class EmitIntrinsicsTests {

        // (Note the ignored non-void call that has no side effects)
        [TestMethod]
        public void Test() => TestEmittedOpcodes(@"
float a = 0;
float b = 1;
float c = 2;
string s = ""hi"";
matrix3x1 u = [1 2 3];
matrix3x1 v = u;
matrix3x1 w = u;

turnstoplayer();

wait(a);
wait(3);
message(a);
spawn();
destroy();
loadbackground(s);
loadbackground(""hi"");
addscript();
addscript(s);
addscript(s, b);
startscript(s);
startscript(s, b);
startscriptmany(s);
startscriptmany(s,b);
depivot();
rotate(a);
setrotation(a);
faceplayer();
a = turnstoplayer();
addspeed(a);
setspeed(a);
gimmick(s);
gimmick(s, b);
gimmick(s, b, c);
print(""hi"");
print(s);
print(3);
print(a);
print(u);
a = random(b,c);
u = random(v,w);
a = sin(b);
u = sin(v);
a = cos(b);
u = cos(v);
a = tan(b);
u = tan(v);
a = asin(b);
u = asin(v);
a = acos(b);
u = acos(v);
a = atan(b);
u = atan(v);
a = atan2(b, c);
u = atan2(v, w);
a = turns2rad(b);
u = turns2rad(v);
a = rad2turns(b);
u = rad2turns(v);
a = ceil(b);
u = ceil(v);
a = floor(b);
u = floor(v);
a = round(b);
u = round(v);
a = abs(b);
u = abs(v);
a = length(b);
a = length(v);
a = distance(b, c);
a = distance(v, w);
a = mrows(u);
a = mcols(u);
", @"
[op]             Set | [f]                a | [f]                0 | --------------------
[op]             Set | [f]                b | [f]                1 | --------------------
[op]             Set | [f]                c | [f]                2 | --------------------
[op]       SetString | [s]                s | [s]             ""hi"" | --------------------
[op]             Set | [f]              u+0 | [f]                1 | --------------------
[op]             Set | [f]              u+1 | [f]                2 | --------------------
[op]             Set | [f]              u+2 | [f]                3 | --------------------
[op]            Set4 | [f]                v | [f]                u | --------------------
[op]            Set4 | [f]                w | [f]                u | --------------------
[op]           Pause | [f]                a | -------------------- | --------------------
[op]           Pause | [f]                3 | -------------------- | --------------------
[op]         Message | [f]                a | -------------------- | --------------------
[op]           Spawn | -------------------- | -------------------- | --------------------
[op]         Destroy | -------------------- | -------------------- | --------------------
[op]  LoadBackground | [s]                s | -------------------- | --------------------
[op]  LoadBackground | [s]             ""hi"" | -------------------- | --------------------
[op]       AddScript | -------------------- | -------------------- | --------------------
[op]       AddScript | [s]                s | -------------------- | --------------------
[op]       AddScript | [s]                s | [f]                b | --------------------
[op]     StartScript | [s]                s | -------------------- | --------------------
[op]     StartScript | [s]                s | [f]                b | --------------------
[op] StartScriptMany | [s]                s | -------------------- | --------------------
[op] StartScriptMany | [s]                s | [f]                b | --------------------
[op]         Depivot | -------------------- | -------------------- | --------------------
[op]     AddRotation | [f]                a | -------------------- | --------------------
[op]     SetRotation | [f]                a | -------------------- | --------------------
[op]      FacePlayer | -------------------- | -------------------- | --------------------
[op]   AngleToPlayer | [f]                a | -------------------- | --------------------
[op]        AddSpeed | [f]                a | -------------------- | --------------------
[op]        SetSpeed | [f]                a | -------------------- | --------------------
[op]         Gimmick | [s]                s | -------------------- | --------------------
[op]         Gimmick | [s]                s | [f]                b | --------------------
[op]         Gimmick | [s]                s | [f]                b | [f]                c
[op]           Print | [s]             ""hi"" | -------------------- | --------------------
[op]           Print | [s]                s | -------------------- | --------------------
[op]           Print | [f]                3 | -------------------- | --------------------
[op]           Print | [f]                a | [f]                1 | [f]                1
[op]           Print | [f]                u | [f]                3 | [f]                1
[op]             Rng | [f]                a | [f]                b | [f]                c
[op]            Rng4 | [f]                u | [f]                v | [f]                w
[op]             Sin | [f]                a | [f]                b | --------------------
[op]            Sin4 | [f]                u | [f]                v | --------------------
[op]             Cos | [f]                a | [f]                b | --------------------
[op]            Cos4 | [f]                u | [f]                v | --------------------
[op]             Tan | [f]                a | [f]                b | --------------------
[op]            Tan4 | [f]                u | [f]                v | --------------------
[op]            Asin | [f]                a | [f]                b | --------------------
[op]           Asin4 | [f]                u | [f]                v | --------------------
[op]            Acos | [f]                a | [f]                b | --------------------
[op]           Acos4 | [f]                u | [f]                v | --------------------
[op]            Atan | [f]                a | [f]                b | --------------------
[op]           Atan4 | [f]                u | [f]                v | --------------------
[op]           Atan2 | [f]                a | [f]                b | [f]                c
[op]          Atan24 | [f]                u | [f]                v | [f]                w
[op]       Angle2Rad | [f]                a | [f]                b | --------------------
[op]      Angle2Rad4 | [f]                u | [f]                v | --------------------
[op]       Rad2Angle | [f]                a | [f]                b | --------------------
[op]      Rad2Angle4 | [f]                u | [f]                v | --------------------
[op]            Ceil | [f]                a | [f]                b | --------------------
[op]           Ceil4 | [f]                u | [f]                v | --------------------
[op]           Floor | [f]                a | [f]                b | --------------------
[op]          Floor4 | [f]                u | [f]                v | --------------------
[op]           Round | [f]                a | [f]                b | --------------------
[op]          Round4 | [f]                u | [f]                v | --------------------
[op]             Abs | [f]                a | [f]                b | --------------------
[op]            Abs4 | [f]                u | [f]                v | --------------------
[op]             Abs | [f]                a | [f]                b | --------------------
[op]         Length4 | [f]                a | [f]                v | --------------------
[op]        Distance | [f]                a | [f]                b | [f]                c
[op]       Distance4 | [f]                a | [f]                v | [f]                w
[op]             Set | [f]                a | [f]                3 | --------------------
[op]             Set | [f]                a | [f]                1 | --------------------
");

    }
}
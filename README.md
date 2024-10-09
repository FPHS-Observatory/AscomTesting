# Testing ASCOM

Just a thing used to test ASCOM parts.
I'll be adding to this stuff.

## Requirements

The NuGet package works great, no need to install it on your end.

You definitely need *.NET Framework 4.7.2* though.

After that it should build just fine. Let me know if it doesn't.

## Other Stuff

I've refactored some of the code. Added comments, whatnot. I'm a little more
verbose here than usual, because it's a test project.

An improved version of what we were working on earlier can be found at
`AscomTesting\TestCameraImageData.cs`, but you can check everything out. The
only files I've modified are that one and `Program.cs`, the other stuff is
generated by the compiler.

This is a Windows Forms app because ASCOM requires it. That means though that
we won't have to do much setup to add UIs if that's what we want to do (might
as well).
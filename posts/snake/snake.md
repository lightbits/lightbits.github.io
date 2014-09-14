![][1]

This is my first 3D game, written using C++ and OpenGL. 
I went for a simple gameplay, but it ended up being complicated after all.

You control the snake with the arrow keys, around the sphere. 
Pick up apples to grow longer and avoid enemies to increase your score.

## Nerdy details
I spent a while figuring out movement along the sphere. I first tried spherical coordinates, with a velocity for the horizontal and vertical angle. This did not give the kind motion I wanted - it was very unintuitive.

So I dropped this and looked back at my notes on physics, and remembered the sentripetal acceleration of a particle in a circular orbit ``a = - dot(v, v) / r``.

To use this I work with positions in world space. The player's acceleration is specified in tangent space, which gives intuitive motion. To transform back to world space I form a tangent frame from the player's velocity, the sphere normal and the cross product of these as the bitangent.

The camera follows the player by using the ``glm::lookAt`` function, with the player's velocity as the up-vector, and position at player offset by a scaled normal.

The wireframe rendering actually uses a special shader, which is described [here][2]. By storing the barycentric coordinates with each vertex attribute, we can calculate how close to the edge we are, using the ```fwidth()`` function, and color accordingly.

## Audio
The sound effects were created with the [bfxr][3] tool, and the bgm was made with [pxTone][4]. This was my first time making music for a video game, and it went as you can tell -_-'.

## Source code and binaries
The game was programmed with C++ 11, so you may have to download and install the Visual C++ Redistributable for Visual Studio 2012 x86, [here][5].

Windows binary: [riemannsnake_windows.7z][6]
Source code: [lightbits/riemannsnake][7]

  [1]: http://1.bp.blogspot.com/-lkeWykpRwbE/UsiGFJWzg-I/AAAAAAAAAIQ/ecE8sQDjbIM/s1600/screenshot0.png
  [2]: http://codeflow.org/entries/2012/aug/02/easy-wireframe-display-with-barycentric-coordinates/
  [3]: http://www.bfxr.net/
  [4]: http://pxtone.haru.gs/
  [5]: http://www.microsoft.com/en-us/download/details.aspx?id=30679
  [6]: https://dl.dropboxusercontent.com/u/27844576/Releases/riemannsnake_windows.7z
  [7]: https://github.com/lightbits/riemannsnake
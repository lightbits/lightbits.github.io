# Two-dimensional Photon Simulator
Browsing the [Tigsource forums](http://forums.tigsource.com/) is always a good idea if you need inspiration. I saw [a post](http://forums.tigsource.com/index.php?topic=31378.0) about simulating photons in a 2D image, and couldn't help but try it out myself.

![500000 photons](/posts/photons/result_500000.png)

The above image simulates half a million photons bouncing around in a scene. It took about 30 secs to render on one thread of an Intel Core i5 @ 1.60 GHz.

The basic idea is simple:

* The scene consists of a light source which emits photons
* Each photon has a color, position and direction, and traverses the scene in fixed steps
* In each step we:
  * Accumulate its color in a lightbuffer
  * Check if it collides with a surface, and emit a photon in a new direction with the color multiplied with the surface
* (Repeat some thousand times for many photons)

Finally, we blend the lightbuffer with the original image by multiplying the pixels together. This means we need three seperate images: a colored texture (unlit scene), a collision texture describing what surfaces are collidable, and a texture to hold the photon colors.

Naturally, the values in the lightbuffer will quickly exceed the range of 8-bit colors, so we need to use High-Dynamic-Range images to get greater range. Because I'm lazy I just pack the pixels into an array of 32-bit floats, one for each R, G and B component. (Yes it uses a lot of RAM...).

Now the problem is that after we multiply the light with the texture, we end up with an image that can't be displayed on todays monitors, as the RGB values are a hundred times greater than 1.0. If we clamp each component to a [0, 1], we get an image which is completely white some places, and completely black otherwise.

![Clamped](/posts/photons/result_clamped.bmp)

In the above image I clamped the lightbuffer to [0, 1] before multiplying with the texture. Clearly not cool at all.

### Queue Reinhard! 

To fix this, we use **tonemapping**. Which is basically a way to compress a HDR image to a displayable image, while preserving relative brightness and color. Reinhard's tonemap operator is probably one of the more popular ones, and it seemed simple to implement, so I went with that.

The idea behind tonemapping is to first find a measure of how bright the overall image is (luminance), and then scale the brightness of each pixel appropriately using that. More advanced techniques calculate local luminance and does a better job at enhancing dark regions, but I didn't go with that.

The first thing we need to do is calculate the **log-average-luminance**:
$$
\bar{L}_w = \exp\left ( \frac{1}{N} \sum_{x, y} \ln(\delta + L_w(x, y)) \right )
$$
That is, we sum up the logarithm of the luminance $L_w$ of each pixel, divide by the number of pixels $N$, and take the inverse logarithm $\exp$. 

Why the log of each luminance, and not just the luminance? I don't know the exact reasoning, but I assume it provides a better measure of the overall brightness.

We now scale the luminance by some exposure value $a$:
$$
L(x, y) = \frac{a}{\bar{L}_w} L_w(x, y)
$$
I use an exposure of about 0.3, which gives a bright enough image. Finally we apply the tonemap operator:
$$
L_d(x, y) = \frac{L(x, y) \left(1 + \frac{L(x, y)}{L_{white}^2} \right)}{1 + L(x, y)}
$$
which compresses the luminance to a displayable range. $L_{white}$ is the smallest luminance that will be mapped to pure white.

The trick now is how to scale the RGB values using the scaled luminance. From what I've read, you can convert from linear RGB into CIE xyY. The advantage of this color space is that the luminance is stored as the Y component, and the chromaticity xy is independent of it. This means you can convert from RGB to xyY, perform tonemapping on the Y value, and convert back to RGB.

A [much simpler approach](http://imdoingitwrong.wordpress.com/tag/hdr/) though, is to calculate the luminance from the RGB values directly, perform tonemapping on this value, and scale the RGB values appropriately:

    vec3 pixel = img.getPixel(x, y);
	float R = pixel.x;
	float G = pixel.y;
	float B = pixel.z;

	// Calculate pixel luminance
	float Lp = 0.2126f * R + 0.7152f * G + 0.0722 * B;

	// Scale the pixel luminance to a middle gray zone
	float L = Lp * exposure / lumAvg;

	// Apply modified tonemapping operator and compress luminance to displayable range
	float Ld = L * (1.0f + (L / (white * white))) / (1.0f + L);

	// Scale and clamp all colors by the relative luminance gain
	float scale = Ld / Lp;
	R = std::min(R * scale, 1.0f);
	G = std::min(G * scale, 1.0f);
	B = std::min(B * scale, 1.0f);
	img.setPixel(x, y, vec3(R, G, B));

Giving that a whirl gives the scene shown at the top. In that scene I set the exposure to 0.3, and the white to 64.0. To prevent unlit places from being completely black, I add a small ambient light to the lightbuffer before simulation.

### Pictures!
Here's it running on a mario level, with a hundred thousand photons:

![mariotthousand](/posts/photons/mariotthousand.png)

Half a million photons:

![mariohmillion](/posts/photons/mariohmillion.png)

One million photons:

![mariomillion](/posts/photons/mariomillion.png)

## Resources
The following links really helped while trying to implement a tonemap operator:

* [The Reinhard tonemapping algorithm](http://www.cs.utah.edu/~reinhard/cdrom/)
* [mynameisjp: A closer look at tonemapping](http://mynameismjp.wordpress.com/2010/04/30/a-closer-look-at-tone-mapping/)
* [imdoingitwrong's blogpost](http://imdoingitwrong.wordpress.com/tag/hdr/)
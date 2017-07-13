v4l2 adventures: Real-time capture for computer vision
============================================================

<i>
For the past few weeks, I've been working on camera capture on Linux for a computer vision project: tracking Roomba robot vacuum cleaners from a drone. This in preparation for our university team's entry in the [International Aerial Robotics Competition (IARC)](http://www.aerialroboticscompetition.org/): an annual competition for autonomous drones. The current challenge has been unsolved since 2014, in which the participating teams need to build a drone that can navigate a 20x20m indoor space by itself, without GPS, and herd ten Roombas from one side to the other through physical interaction.
</i>

![](venue1.jpg)

Due to size constraints, the drones entering this competition tend to go with a low-end on-board computer, such as the Odroid, the Intel NUC, or lately the nvidia jetson family. These are affordable and small embedded devices that can fit comfortably on a small to medium sized drone, while still being decent enough to do on-board image processing. I say low-end, but these things are beasts compared to what we had ten or twenty years ago, of the same size and price. However, they are still weak enough that you need to be careful with your software: how fast you can process stuff puts a hard limit on the maneuvers and speeds your drone can go through.

For example, we use a 180 degree, 60 FPS fisheye-lens camera to track Roombas seen below our drone. The algorithm is not fancy: just your run-of-the-mill color thresholding with some shady low-pass filtering thrown on top.

<div style="margin: 0 auto;"><img id="image_step" width="320px" src="cc0.jpg"></img></div>
<div style="width: 80%; margin: 0 auto; text-align: center;"><p id="image_desc">Input image (downsampled a couple of times)</p></div>
<div style="display: flex; justify-content:space-between; max-width: 50%; margin: 0 auto;">
    <a href="javascript:;" onclick="document.getElementById('image_step').src='cc0.jpg'; document.getElementById('image_desc').innerText='Input image (downsampled a couple of times)';">Step 1</a>
    <a href="javascript:;" onclick="document.getElementById('image_step').src='cc4.jpg'; document.getElementById('image_desc').innerText='Convert to normalized RGB (getting rid of diffuse lighting variations)';">Step 2</a>
    <a href="javascript:;" onclick="document.getElementById('image_step').src='cc1.jpg'; document.getElementById('image_desc').innerText='Convert to binary image by thresholding red and green pixels';">Step 3</a>
    <a href="javascript:;" onclick="document.getElementById('image_step').src='cc2.jpg'; document.getElementById('image_desc').innerText='Find connected components and their bounding boxes';">Step 4</a>
</div>

The simplicity of the approach was a result of two factors: one being that we found that across all the complicated solutions we tried the color thresholding worked most consistently out of the bunch; and two being that we needed something that would run fast enough: luckily, thresholding is an ancient technique that has benefitted particularly well from the improvement of hardware, to the point where it can run at 60 Hz on the ARM Cortex-A15 (used on the Odroid XU4). However, getting everything to run at 60 Hz required us to pick apart solutions for capturing video, and resulted in me learning a few tips and tricks to make it fast.

Buffers
-------

In v4l2 there are several modes by which you can read frames from the camera, but the one we will look at is the mmap method. In the mmap method, you and the camera negotiate a set of shared buffers that the camera will write to and you will read from. By sharing them, using mmap (memory mapping), you avoid potentially a expensive memory copy  from the camera to your application.

But, in setting up the mmap method, you have a choice of how many buffers to use. Initially, I didn't know why I might care about that, and instead of reading the manual, I just left it at whatever number was in the example code I was copying. It would be a whole year later before I realized the purpose of these buffers, and also that the example code was not suited for our real-time computer vision purposes.

I had neglected reading the manual because it seemed verbose and hard to decipher, but here are the basic rules that you need to keep in mind:

* The camera needs buffers to do its writing.
* You give it a buffer for writing by queuing it.
* You get a buffer for reading by dequeuing it.
* When you dequeue a buffer the camera can no longer write to it.
* The camera does not overwrite buffers: a buffer must be dequeued and queued again to be available for writing.

In the example code our team was using, the code would request a number of buffers (3 by default), queue them all, and tell the camera to start recording. It would then enter a loop where it waits for a new frame, dequeues it, and makes it available for processing (i.e. computer vision or logging). After processing, the buffer would be requeued.

Given the rules above, the cause for our problems becomes evident: if the processing time per frame is too long, the camera runs out of buffers to write to. This leads to two effects: one being that subsequent frames are dropped, because the camera is not allowed to overwrite buffers already written to; they must be dequeued and requeued first.

The second effect is that in the next loop iteration, the buffer that is dequeued is not necessarily the latest one. In fact, on all the cameras I tested with, the order of dequeuing went from oldest to newest: i.e. if you queue buffers a, b, c, in that order, then the camera will write to them in the same order, and subsequent dequeues will give a, b, c.

This was not what we wanted: in our case, we don't care about old frames, we just want the latest information. For example, if you have an object detection algorithm that works on stand-alone frames, you will only care about the most recent one, because it gives you the most recent position of the object, and also because your algorithm does not keep track of state across frames. (On the other hand, if your algorithm does some sort of tracking across frames, you might want the deltatime between frames to be consistent: i.e. by never skipping frames).

The solution to these problems was simple: to get the most recent frame, make sure to dequeue *all* buffers and choose the most recent one. Depending on the camera driver, this can be done by dequeuing buffers until there are no more available, which can be checked using linux file descriptors and polling. If you, for some reason, don't get buffers in a chronological order, you might need to compare timestamps as well.

Then, after picking the most recent buffer, queue *all* the buffers again. In this sense, the number of queued buffers represents how much leeway your algorithm has in its processing time. You want to make sure you queue enough buffers for the camera to use while you are busy processing.

Let's look at an example with three buffers: <span style="background:#D04648;color:#fff;padding:0 3px 0 3px; border-radius: 3px;">buffer 1</span> <span style="background:#519DFF;color:#fff;padding:0 3px 0 3px; border-radius: 3px;">buffer 2</span> <span style="background:#95AB63;color:#fff;padding:0 3px 0 3px; border-radius: 3px;">buffer 3</span>. Right after starting capture, we have queued all the buffers. The first buffer we dequeue is buffer 1. We then take a long time processing this frame:

![](buffers1.png)

In fact, we spent so much time, that the camera wrote to buffer 2 and 3 while we were busy. When it was time to write the fourth frame, the camera has no more buffers to use, and, since it will not overwrite previous buffers, the frame is dropped.

![](buffers2.png)

The next time we dequeue a buffer, we then get buffer 2; which is not the latest buffer, nor the latest frame we could have gotten had we allocated more buffers. When dequeuing, we also requeue the buffer we just finished processing. But the camera will not write into this until the next frame again.

In summary, to ensure that we get the latest frame possible, we need to:

* Allocate as many buffers as we predict can get filled during our processing time
* Dequeue all the buffers before we start processing, pick the latest one, and requeue all the remaining

Fast JPEG decompression
-----------------------


Some cameras output in a somewhat raw format that can be converted to RGB quickly: i.e. the Bayer format, where each pixel contains information for one color channel only, and the other two channels must be reconstructed from its neighbours. Another format is YUV, where the bits per pixel is reduced by taking advantage of properties of human perception and operating in a different color space: i.e. YUV 4:2:2 uses four bits for brightness, and two bits for two color components (a technique known as [chroma subsampling](https://en.wikipedia.org/wiki/Chroma_subsampling)), for a total of 8 bits per pixel.

But, some cameras only give you the option of JPEG output. In this case, you want to decompress it as fast as possible. In my search I found two libraries that are particularly interesting: stb_image, and turbojpeg.

stb_image is super easy to use on any OS, and is great for prototypes. But, since it's not as fast as turbojpeg, I would not use it for real-time decompression. Unfortunately, using turbojpeg is more involved than simply downloading a file from github and dropping it into your source directory. But the speed boost you get is worth the hassle. Below are some tips for increasing the speed even further.

Make turbojpeg do downsampling during decompression
---------------------------------------------------

If your computer vision algorithm downsamples the image before processing it --- such as in a neural network, or in the Roomba detector above --- you can specify the desired resolution to turbojpeg, which will include it while decompressing. Compared to decompressing in full resolution and downsampling afterwards, this significantly reduces decompression time and overall preprocessing time. Plus, there's less code, since you don't need the downsampling!

As a general computer vision tip, you should also consider if you *can* downsample the image. Maybe the results are still good, or maybe even better due to the inherent blurring that is done?

Do you really need RGB?
-----------------------
Another thing that can speed up decompression is to avoid generating the RGB output. JPEG store the image data in a format known as YUV that, roughly speaking, describes brightness (Y) and hue (UV). If you only need a grayscale image, you can use the Y value. If your algorithm operates on color values, maybe UV would be a better space, since it is seperated from brightness.

You can tell turbojpeg to avoid generating the RGB values by passing TJPF_YUV (todo?) into the decompression function.

Code
----

I had a hard time finding out how to actually use turbojpeg: all google gave me were some example code from StackOverflow, and searching for the function names did not lead me to their documentation page. But, here's a [link to it](http://www.libjpeg-turbo.org/Documentation/Documentation) nonetheless, under TurboJPEG C API.

If you read this for code snippets you can check out the [usbcam](github.com/lightbits/usbcam) repository, which implements the buffer strategy I discussed above, and also contains a small function that does JPEG to RGB conversion using turbojpeg.

Thanks for reading!

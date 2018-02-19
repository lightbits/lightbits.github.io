# Rotations and mathematical hammers: Part 2

The take-away from last time was that Euler angles can *gimbal lock*, where you lose the ability to rotate around all three axes: adjusting any angle in isolation only gives you two distinct motions. This causes gradient descent, and similar optimization strategies, to slow to a stop, or adjust the wrong parameters.

Another way of seeing it is that Euler angles suck at tracking *absolute* orientation.

See, when I coded this 3D book model, I inadvertently chose its default orientation (all angles zero) to be with its cover facing the camera:

![](model1.png)

This happens to have an impact on gimbal lock: for this choice, we have all three degrees of freedom when the cover is facing the camera, but not when the book is sideways. On the other hand, if the default orientation had been sideways....

![](model2.png)

we would have three degrees of freedom at the sideways orientation, but *not* when the cover is facing the camera.

No matter which default orientation we base our Euler angles around, we will run into gimbal lock sufficiently far away. But they are OK as long as we stay close to the zero.

<br>
<br>
<br>
# The Tumbler

3D modelling software have tackled similar problems for a long time: how can the user, with their 2D mouse interface, rotate an object in 3D?

One solution is called the *Tumbler*. It is notoriously unintuitive and the only excuse you get for using it is [not knowing any better](https://www.mattkeeter.com/projects/rotation/). It works like this: when you click and drag your mouse horizontally or vertically, it adjusts either of two Euler angles and rotates the thing, but when you let go, this orientation is saved and the Euler angles are reset to zero.

<style>
.slider img {
    display:inline;
    max-width:none;
    padding:0;
    margin:0;
}
.slider {
    display:inline-block;
    overflow-y:hidden;
    overflow-x:hidden;
    border:1px solid #ccc;
}
.slider-wrap { width:fit-content; margin:0 auto; }
input { vertical-align: middle; }
</style>
<div class="slider-wrap">
    <div class="slider" id="slider4" style="max-width:240px;max-height:260px;">
        <div style="width:1700px;"><img src="gimbals1.png"/><img src="gimbals1-2.png"/><img src="gimbals2.png"/><img src="gimbals3.png"/><img src="gimbals3-4.png"/><img src="gimbals4.png"/><img src="gimbals5.png"/></div>
    </div>
    <br>
    <input type="range" min=0 max=6 step=1 value=0 oninput="document.getElementById('slider4').scrollLeft = this.value*240;"></input>
    <label>Click and drag</label>
</div>

The coordinate frame you rotate around follows the object while you're rotating it, but it resets when you release the mouse button. So no matter how much you have rotated the object in the past, when you click and drag your mouse up and down, or left and right, it behaves the same as the first time.

It turns out that this is a **terrible** user interface, because, even though the object can theoretically be rotated in three distinct ways anywhere you start rotating, the mouse's lack of a third dimension keeps you from accessing more than two. For us though it is a great solution to our gimbal lock problem.

Notice how dragging the Tumbler, without letting go, is like our first strategy of accumulating small angle increments from gradient descent (the difference being that gradient descent is not limited by a two-dimensional mouse). This runs into gimbal lock if we drag it too far, but not if we let go before the Euler angles get too big.

We can extend this idea to gradient descent: instead of accumulating increments into a set of global angles, we apply the rotation they represent to the object's currently saved orientation which we store, for example, as a rotation matrix.

This little change makes all the difference. See, when we computed the gradient last time, we added or subtracted a delta around global Euler angles, like so:

<pre><code>dedrx = <span style="color:#999;">(E(euler(</span><span id="efg">rx+drx</span><span style="color:#999;">,ry,rz), T) -
         E(euler(</span><span id="efg">rx-drx</span><span style="color:#999;">,ry,rz), T)) / 2drx</span>
dedry = <span style="color:#999;">(E(euler(rx,</span><span id="efg">ry+dry</span><span style="color:#999;">,rz), T) -
         E(euler(rx,</span><span id="efg">ry-dry</span><span style="color:#999;">,rz), T)) / 2dry</span>
dedrz = <span style="color:#999;">(E(euler(rx,ry,</span><span id="efg">rz+drz</span><span style="color:#999;">), T) -
         E(euler(rx,ry,</span><span id="efg">rz-drz</span><span style="color:#999;">), T)) / 2drz</span></code></pre>

<!--
    dedrx = (E(euler(rx+drx,ry,rz), T) -
             E(euler(rx-drx,ry,rz), T)) / 2drx
    dedry = (E(euler(rx,ry+dry,rz), T) -
             E(euler(rx,ry-dry,rz), T)) / 2dry
    dedrz = (E(euler(rx,ry,rz+drz), T) -
             E(euler(rx,ry,rz-drz), T)) / 2drz
 -->
If the global angles were at a particular point (the `euler` matrix was close to gimbal lock) adding or subtracting a delta would not have the effect we wanted. But now we can compute the gradient by adding or subtracting a delta around zero, and applying that to the current rotation matrix:

<pre><code><span style="color:#999;"><span style="color:#000;">dedrx = </span>(E(euler(<span style="color:#000;">0+drx</span>,0,0)<span style="color:#000;">*R</span>, T) -
         E(euler(<span style="color:#000;">0-drx</span>,0,0)<span style="color:#000;">*R</span>, T)) / 2drx
<span style="color:#000;">dedry = </span>(E(euler(0,<span style="color:#000;">0+dry</span>,0)<span style="color:#000;">*R</span>, T) -
         E(euler(0,<span style="color:#000;">0-dry</span>,0)<span style="color:#000;">*R</span>, T)) / 2dry
<span style="color:#000;">dedrz = </span>(E(euler(0,0,<span style="color:#000;">0+drz</span>)<span style="color:#000;">*R</span>, T) -
         E(euler(0,0,<span style="color:#000;">0-drz</span>)<span style="color:#000;">*R</span>, T)) / 2drz</span></code></pre>

The gradient gives us a "direction" to rotate in, and, like before, we can turn that into three angles `rx,ry,rz`. But instead of accumulating those into three global angles, we update the orientation like this:

    R = euler(rx,ry,rz) * R

As long as the amount we rotate by is small, these angles will be close to zero, and the euler matrix behaves nicely.

<!-- <p style="color:#999;">
We could also use unit-length quaternions to track orientation. They are often preferred because they use fewer bytes than rotation matrices and, like rotation matrices, they do not gimbal lock. But they also have constraints to keep them valid (must be unit-length), so we can't freely adjust its parameters to find a direction for gradient descent.
</p> -->

<br>
<br>
<br>
# Looking closely

![](euler-random-big.png)
<br>

You may be asking why we used that particular Euler angle sequence; maybe a different one would be better?

The above image compares the two most popular sequences. Each cube is rotated by three random angles. The gray cubes are rotated by the same angles about the same axes, but in a different order.

It clearly looks like a mess, none of the cubes are alike. So if we were to update our orientation using one or the other, we could get completely different results!

    R = euler1(rx,ry,rz)*R // this would be completely different from
    R = euler2(rx,ry,rz)*R // this

But let's look more closely around the area that we're interested in, small angles, say, within plus or minus 20 degrees around zero.

![](euler-random-small.png)

Now we almost can't tell them apart! But why?

<br>
<br>
<!-- # The maths -->
<!-- <br> -->

Let's look at the actual maths behind these rotations:
 <!-- If we multiply together the rotation matrices for a ZYX rotation we get this: -->

![](eq1.png)

<!--
    | cy*cz   cz*sx*sy - cx*sz   sx*sz + cx*cz*sy |
    | cy*sz   cx*cz + sx*sy*sz   cx*sy*sz - cz*sx |
    |   -sy              cy*sx              cx*cy |
 -->

Ok it's horrible.

But a trig fact tells us that for small angles cos(*x*) = 1 and sin(*x*) = *x*,  so we can say that the above monstrosity is almost equal to this:

![](eq2.png)

<!--
    |  1      x*y - z    x*z + y |
    |  z    x*y*z + 1    y*z - x |
    | -y            x          1 |
 -->

And if we multiply two small numbers together, the product becomes *really* small compared to any one of them alone, so we get:

![](eq3.png)

<!--
    |  1   -z    y |
    |  z    1   -x |
    | -y    x    1 |
 -->

Ok, here is the exciting part: if you repeat the above steps, you will find that *any Euler angle sequence becomes equal to this matrix*. This means that it wouldn't matter which one we choose to update our orientation, they would have pretty much the same effect.

Neat! But isn't that also super suspicious? No? How about this...

<br>
<br>
<!-- # Axis-angle -->

Euler angles are three angles about three axes, but we can also parametrize our rotation in terms of one axis and one angle around it. There's even a formula to convert that to a rotation matrix:

![](eq4.png)
<!-- R = I + sin(a) skew(r) + (1-cos(a)) skew(r)^2 -->

<p style="color:#999;">*a* is the angle and *r* is the axis. We'll see what this *skew* function is soon</p>

This is not minimal because it uses four numbers, but if we multiply the angle into the axis we do get a minimal parametrization: a vector whose length is the original angle and, when normalized, is the original axis. Let's rewrite our formula in terms of this vector:

![](eq5.png)
<!-- R = I + sin(|w|) skew(w/|w|) + (1-cos(|w|)) skew(w/|w|)^2 -->

If the angle (length of *w*) is small, some things cancel and we're left with:

![](eq6.png)
<!-- R = I + skew(w) -->

skew(*w*) is called the skew-symmetric form of *w*, and is the matrix that, when multiplied with a vector, gives you the cross product between *w* and that vector.

![](eq7.png)
<!--
                    |  0   -z    y |
    skew([x,y,z]) = |  z    0   -x |
                    | -y    x    0 |
-->
which means that
![](eq8.png)
<!--
        |  1   -z    y |
    R = |  z    1   -x |
        | -y    x    1 |
-->

Well how about that, it's the same matrix as before!

<br>
<br>

To recap what's going on, we first represented a rotation as three numbers describing Euler angles, and no matter what sequence we interpreted them to be, if the angles were small, we got the above matrix. We then looked at using three numbers describing an angle and an axis. Again, if the angle was small, we got the same thing.

Although we can assign entirely different meanings to these three numbers (an axis-angle or any Euler sequence)&mdash;and for big angles they look entirely different too!&mdash;they are all equal to each other and this weird rotation matrix.

<br>
<br>
<br>
# Physics

It seems like there is a "canonical" small rotation, that all forms of rotations tend towards.

To intuitively appreciate this, let's first look at stuff in two dimensions.
![](physics2.png)
In physics, you may have learned that a point rotating on a circle has a velocity tangent to the circle, and that the speed is proportional to the angular speed and the radius: *v = wr*.

![](physics1.png)

We could also, more generally, say that the velocity is the cross product between an *angular velocity vector*, pointing in or out of the page, and the position: *v = w* x *r*.

<!-- The right-hand rule lets you figure out the direction of the angular velocity vector by wrapping your right hand along the rotation. Your thumb will then either point away from or into the page. -->

![](physics3.png)

The latter also holds in 3D, now with the point rotating in a plane perpendicular to the angular velocity vector, which can be an arbitrary direction; not just in or out of the page. I bring this up because rotation matrices can be seen as a set of three vectors, defining the three axes of a coordinate system:

![](eq9.png)

<!-- R = [X | Y | Z] -->
What we did earlier was to rotate this matrix by a small Euler angle offset, which we wrote as a matrix-matrix product. But we can expand that and multiply each vector inside:

![](eq10.png)

<!-- R = euler*R = [euler*X | euler*Y | euler*Z] -->
We also saw that the euler (and axis-angle) matrix, for small angles, was equal to:

![](eq11.png)

<!-- I + skew(w) -->
Remember that skew(*w*), when multiplied by a vector, gives the cross product between *w* and that vector. So if we put that back into the above we get:

![](eq12.png)

<!-- R = R + [w cross X | w cross Y | w cross Z] -->
Which looks a lot like adding, to the current orientation, the tangential velocity of each axis rotating on a circle, with a speed and direction defined by *w*.

From a physics point of view, in the same way that a point rotating on a circle has a velocity tangent to it, each axis in the coordinate frame does too, and we find it by taking the cross product between the angular velocity and the axis.

The weird thing, though, is that *any* rotation made small enough, is essentially no different from an angular velocity vector, and they are all *the same* angular velocity vector.
<!-- It's the analog of linearizing a translation; but in rotation space -->

<br>
<br>
# The mathematics of things that look similar

What is this w thing, these three numbers? We've called them Euler angles and axis-angle, but if you make them small they describe the same thing? Why is rotation so annoying when translation is so easy? What's special about it?

Mathematicians thought about these things 150 years ago, and decided to invent what we now call Lie groups&mdash;which is a part of group theory, which is about defining very precisely how stuff that look similar are, in fact, similar (for some definition of similar).

They promptly went ahead and gave weird names to everything. Rotations they call *SO3* and *w* they call *so3* <span style="color:#999;">(yes, lower case matters and yes, I agree)</span>. Then other people wrote books explaining what the names mean. One of them has a cover that says *State Estimation for Robotics* in large, bold letters, with *A Matrix Lie Group Approach* underneath. The other is called *Robotics, Vision and Control.* *Fundamental algorithms in MATLAB*.

![](blobs-and-arrows.png)
<p style="text-align:center;color:#999;">Copied from Robotics, Vision and Control: Appendix D - Lie Groups and Algebras.</p>

Unfortunately, after reading them, I can't tell you why rotations are strange; it supposedly has to do with translations living in something called a vector space, and that rotations do not, but if you ask me why rotations don't live in a vector space I can't give you an answer; I can only tell you that SO3 are big rotations and so3 are small rotations, whatever that means.

If you'd like to dig into this topic, you can check out those two books, or look for other ones. Either way, I hope this article has revealed a bit of the motivation behind it all and given you some visualizations to relate things back to: how rotations are surprisingly hard to deal with, and the strange connection between seemingly different representations.

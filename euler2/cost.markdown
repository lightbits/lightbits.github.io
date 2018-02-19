Aside: Reducing computational cost
----------------------------------

So far I've kept an aggressively *positive* attitude towards laziness and inefficiency. But sometimes, like when your algorithm takes an entire day to run, you don't need to write code faster, you need to write faster code. So let's break down our solution in terms of what the computer has to do.

<pre style="margin:0 8px -8px 0;float:left;width:41%;"><code style="font-size:60%;">update_parameters(R, T):
  dedrx = (E(euler(+drx,0,0)*R, T) -
          E(euler(-drx,0,0)*R, T))/2drx
  dedry = (E(euler(0,+dry,0)*R, T) -
          E(euler(0,-dry,0)*R, T))/2dry
  dedrz = (E(euler(0,0,+drz)*R, T) -
          E(euler(0,0,-drz)*R, T))/2drz
  dedtx = (E(R, T+[dtx,0,0]) -
          E(R, T-[dtx,0,0]))/2dtx
  dedty = (E(R, T+[0,dty,0]) -
          E(R, T-[0,dty,0]))/2dty
  dedtz = (E(R, T+[0,0,dtz]) -
          E(R, T-[0,0,dtz]))/2dtz

  rx = -gain*dedrx
  ry = -gain*dedry
  rz = -gain*dedrz
  R = euler(rx,ry,rz)*R
  T -= gain*[dedtx, dedty, dedtz]
</code></pre>

Maybe most prominently, each optimization step will evaluate the error function E twelve times. In our toy example this is not a big deal because it was a loop over five-or-so 2D-3D correspondences with not a lot of work each. But let's look at a real example...

*Direct Sparse Odometry* is an algorithm designed to track camera motion. At its core, what it does is the same as in our book problem, but instead of a book, they have a 3D model of the world. Similar to how we try to find how the book is positioned in a photo, they try to find how the world moved between frames (assuming it is caused by the camera moving).

![](dso.jpg)

<p style="max-width:500px;margin:0 auto;color:#999;">
A figure from Direct Sparse Odometry paper showing color-coded depth maps. In addition to estimating the camera pose over time, they also estimate the depth maps themselves, in a process called bundle adjustment (called so because it adjusts the entire bundle of parameters: points *and* cameras.)
</p>

Just like we use an error function to judge how good our alignment was, they use an error function to judge how well the world model fits with the photo, at a given translation and rotation.

There's one key difference though: our book model had five points, but their model can have as many points as there are pixels in an image. Each of these don't involve that much work: a camera projection, and a subtraction of two pixel patches. But it's a lot of work for an inner loop over 100k+ pixels, especially if the loop runs twelve times over!

This means that they have to do some tricks to speed things up.

## Ways to optimize our optimization

Our book model had five points, but their model can have as many points as there are pixels in an image. Looping over all of those can be prohibitively slow if done twelve times per step, even if the work done per point is small&mdash;as a reminder, this is what our error function looks like:

     E(R, T):
         e = 0
         for u,v,p in patches
             u_est,v_est = camera_projection(R*p + T)
             du = u_est-u
             dv = v_est-v
             e += du*du + dv*dv
         return e / num_patches

The first transformation we can make is to exploit the fact that the derivative of a sum is the sum of derivatives: instead of calling E twelve times, we can call it once and return all derivatives along with it. Something like this:

     E(R, T):
         e = 0, dedrx = 0, dedry = 0, ... dedtz = 0
         for u,v,p in patches
             // something...
         return [e, dedrx, dedry, ..., dedtz] / num_patches

That could be faster? We could differentiate each term in the sum as we did for the sum itself, using finite differences, or maybe we want to use a library with automatic differentiation? Maybe we want analytic derivatives this time? In fact, maybe we shouldn't rewrite this error function at all. Maybe we've SIMD-optimized it so even if we do call it twelve times it's faster than the alternatives?

 <!-- Maybe you are using a different optimization method, like Gauss-Newton or Levenberg-Marquardt, in which case you may want to ... -->

 <!-- <p style="text-align:center;font-size:300%;">...</p> -->

<p style="color:#999;">
To be honest, the premise of this section is a cover story. I'm using it as an excuse to take you on a wild mathematical tangent, but you can use it as a rope, if you want, to reel yourself back into the world of practicality (or just tug on it to reassure yourself that it still exists), if you lose your way inside the abstract manifolds of rotation space.
</p>

There are many alternative routes to go; which one is best depends on your specific problem.

     u_est,v_est = camera_projection(R*p + T)
     du = u_est-u
     dv = v_est-v
     e += du*du + dv*dv

u and v are constant (with respect to our parameters, not the loop).

     2*u_est*diff(u_est,[R,T]) + 2*v_est*diff(v_est,[R,T])

u_est and v_est are some functions f1 and f2:

     u_est = f1(R*p + T)
     v_est = f2(R*p + T)

Let's go on a mathematical detour and see what the latter routes would involve (automatic or analytic).

     euler = Rz(ez)Ry(ey)Rx(ex)

Could do FD, but that can be slow.

Automatic differentiation or analytic derivatives both involve a bunch of cos and sin.

Can mix FD with AD, chain rule.

But we have additional knowledge: euler angles close to zero.

For small values of x: `cos(x) = 1` and `sin(x) = x`.

What's a small value? It's almost wasteful to use the full trig terms when the difference is so small!

So we can replace our `euler` function with this matrix, bearing in mind that it's only valid for small values.

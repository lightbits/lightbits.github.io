# Seperation principles and nonlinear observers

## Summary of the lecture

In the lecture we worked with some problems I had prepared for these two papers:

* Fossen, T. and Strand, J.: *"Passive nonlinear observer design for ships using Lyapunov methods: full-scale experiments with a supply vessel"*
* Loria A.; Fossen, T. and Panteley, E.: *"A separation principle for dynamic positioning of ships: theoretical and experimental results"*

(published in 1999 and 2000, respectively). The gist of it is that a nonlinear observer is a way to estimate stuff, where you don't really care about estimating your uncertainty: the reasoning is something like "you're not modelling the real uncertainty anyway, so let's not even bother". (for example, uncertainty in data association).

The seperation principle is about breaking a big problem into smaller problems that can each be analyzed seperately. Handy!

## asd

Here's a thing I worked on two years ago. It's the computer vision part of a position controller for a drone. It takes an image from a fisheye camera and spits out its position within a grid on the floor.

<video controls style="width: 100%; max-width: 640px; display:block; margin: 0 auto;">
    <source poster="seperation-0.png" src="C:/Writing/phdmeetup/grid_lab.mp4" type="video/mp4"/>
</video>

It was a tricky problem because I couldn't make assumptions about what the environment would look like. All I had to go on was that a human would be able to see a white grid with 1x1 meter tiles.

![](C:/Writing/uavmeetup/out1.jpg)

Other than that, it could be in a gym, surrounded by other line markings for basketball and stuff, or it could be on a noisy carpet. And there would surely be people and safety nets around the perimeter of the grid.

![](C:/Writing/uavmeetup/out4.jpg)

The key that made this work well, or rather, what gave me confidence to say *"yes, it will work here"* or *"no, it's not safe, it will not work in this environment"*, is a seperation principle.

What's neat about this (aside from its remarkable stability, efficiency and success ratio of 100% over the past two missions), is that **if a set of conditions are true, I can guarantee that it gives the true position** (within noise caused by the camera sensor and the IMU).

I'm not going to go through all of them, but to give you a taste, it's like this:

* The grid is detected given atleast four line observations belonging to the grid that form a sufficiently 1x1 meter square.

* A line is detected if it has sufficiently many edge pixels in a sufficiently straight line.

* An edge pixel is detected if the magnitude of the image gradient at that pixel is sufficiently large.

When I say "sufficiently" I mean that some metric is greater than some threshold I compare against in the source code. Like the number of pixels is greater than 1000, or the average distance for all these points from an ideal line is within 10 pixels.

Like I said this isn't everything, but I just wanted to give you a picture. The output is deterministic in the sense that *if* four lines of the grid were detected, I can guarantee that the grid is detected. *If* a line is sufficiently strong against the background, then I can guarantee that the line is detected, and so on.

I can formally prove that it gives the right answer. Now, that may not actually be true because I never did formally prove it. I mean it's obviously mostly true, because it's worked so far, but I think you could go into the source code, or rewrite it mathematically, and verify that if such and such condition is true, then so and so will be within this range, and the variable at the end will, guaranteed, be equal to the true state.

My point is: **This is a stability proof.**

![](seperation-1.png)

It's very different from the sort of stability proof you find in Fossen's paper or in nonlinear control theory. It doesn't use Lyapunov functions or the KYP lemma, it doesn't say that the algorithm is globally exponentially stable; but it basically says the same thing: that the estimate is guaranteed to be close to the true value (c.f. error converges exponentially to zero).

![](seperation-3.png)

Those conditions that I need to be true, for the grid to be detected, are like the assumptions that Fossen makes for their main proof. Like, they assume they know the mass and damping matrices perfectly, that the mass matrix is symmetric, no measurement noise, and so on.

Related: When is the state observable? When 4 grid lines are visible? When they stand out against background? When they are sufficiently white (and the lighting is right)?

I would argue that this 1000-line function is a nonlinear observer. I mean, if you read the code, you will find no resemblance what so ever to the sorts of equations in Fossen's paper, even when implemented in MATLAB. I have lots of weird for loops over lists, and there's this Hough transform thing and points vote for the existence of lines. Lots of weird, non-differential equations stuff.

![](seperation-2.png)

But you can rewrite it as equations. They may be very long and ugly equations involving many numbers, but they would nonetheless be equations. And they would be nonlinear. **This is an observer, it is nonlinear, it is a nonlinear observer.**

My message is that I think nonlinear observer don't need to look like this:

![](seperation-4.png)

And stability proofs don't need to look like this

![](seperation-6.png)

# Cascade

Input = Image
Output = Pose

1280x720x3x255 input
6x1 output

How can we design a stable nonlinear observer?

Lots of dimensionality reductions along the way
Each step makes an assumption as to how to preserve stability
In practice it is hard to ensure formal stability along each step
Want intuitive predictability
Intuitive qualities that are preserved
    e.g. so that you can, after a test, determine if the system is likely to perform as it did during the test for the remainder of the scene. As long as the images preserve these qualities, yes.

Step 1: 1280x720x255x3 -> 640x360x255x3
    Downscale image. Assume grid is sufficiently thick or camera not too far away. Intuitive quality (need to fly high enough).

Step 2: 640x360x255x3 -> 640x360x255
    Make monochrome. Sorta like grayscale, but look for "whiteness".
    White is 255, black is 0. Red is not 255...
    Assumption: grid is white and preserved after this transformation.

Step 3: 640x360x255 -> 640x360
    Look for edges. Pixels at brightness discontinuities. Compare brightness difference against threshold.
    Assumption: Grid stands out against the background.

Step 4: 640x360 -> 128x128x2
    Hough transform. Look for lines. Line has two parameters, angle and offset. Discretize space into 128 buckets for each parameter.
    Assumption: Edges of grid are straight.

Step 5: 128x128x2 -> 20x2
    Choose 20 strongest lines.
    Assumption: Grid lines are dominant.

Step 6: 20x2 -> 4x2
    Find a set of 4 lines that make up a square.
    Assumption: A square is visible and sufficiently square-ish (depends on imu noise)

Step 7: 4x2 -> 6x1
    Compute pose!

Computer vision is starting to make its way into more and more systems, but unfortunately there is a gap between our expectations of what these systems can do, and how robust they actually are in real-life scenarios.
    Very complicated input.
    Researchers completely neglect that NIGHT is a thing. Pretend that it doesn't exist.
    Weather oh no...

    Can we get closer to the stability proofs ála Fossen?
    Intuitive predictability?

    As an engineer applying some computer vision software, you want to be able to give your team guarantees. Yes, as long as THESE QUALITIES are preserved, the system will work.

    Those qualities have to be humanly interpretable. You can't go up and say, well it will work as long as the l2 norm of the difference between these million-dimensional images is less than 0.3...
    That's not ACTIONABLE!

    If you can say, the color needs to be atleast this bright; there should not be other potential things that look like lines; the lines should be atleast this thick; etc.

    then that is a lot better.

Take SLAM or visual odometry, for example.
    You can pick up a paper and read its abstract and think, yeah, that sounds good. A drone autonomously flying around, mapping its environment, planning paths. Great!

    But for some reason, we *don't* have drones flying around path planning and autonomously, outside youtube videos. The reason is not obvious, but I think part of it is that this software is extremely complicated, and contains a bunch of subtle assumptions, deep inside some for loop, not made explicit in the paper, that breaks against reality.

    > SVO. If this carpet wasn't here, the system would crash.

    We try to compensate for this. "It is robust". Trust me, I use the huber norm, so it's robust. Different descriptors.

    I think we should strive to make assumptions clear, and work on building systems that can work under intuitive, predictable, if limited, environments. Because then you can read the paper, see if the conditions of use match your scenario, and say yes/no this will work.

Davison on the matter [futuremapping]
    Benchmarks for SLAM have been unsatisfactory be-
    cause they make assumptions about the scene type and
    shape, camera and other sensor choices and placement,
    frame-rate and resolution, etc., and focus in on certain eva-
    lution aspects such as accuracy while downplaying other
    arguably more important ones such as efficiency or ro-
    bustness. For instance, many papers evaluating algorithms
    against accuracy benchmarks make choices among the test
    sequences available in a dataset such as [52] and report per-
    formance only on those where they basically ‘work’.

# Seperation principle for computer vision

I say that we use nonlinear observers and seperation principles at Ascend, but that's not entirely true because I didn't actually know that we did until I read these papers.

The seperation principle is somewhat different: Instead of seperating a controller and an observer, it's seperating an observer from another observer, and then another observer, and so on.

This is hugely important in computer vision, because your input is really high-dimensional, but the estimate is really low-dimensional, so somewhere along the way you need to do a reduction. And of course, you want to make sure that the estimate is guaranteed to be close to the true value, under certain (REASONABLE) assumptions.

One way to do this is to design a neural network, like a convnet, that takes your 1280x720x3 input and computes the right answer through a series of magical operations that successively reduce the dimension.

If there's anything that the machine learning community has learned, it's that reasoning about the stability (or rather the predictability) of neural networks is hard. We humans can't intuitively reason about how the output will behave across all possible inputs. That has lead to all sorts of bad stuff, like "adverserial attacks" where you can 3D-print a turtle, modify its appearance a little bit, and fool a neural network into thinking it's a gun. From any angle.

![](turtle)
<!-- 34C3 - Deep Learning Blindspots @14:30 -->
<!-- watch that lecture if you're interested. the turtle is recent work, adverserial attacks are still a huge problem. -->
<!-- Sure, if you sit down, you can analyze exactly *why* the network thinks this is a gun. It's not unexplainable. But it's not intuitively explainable. That's my problem with neural networks. -->

![](dronet.png)

This looks cool right? A drone learns to drive, it can follow traffic rules and safely avoid pedestrians and other vehicles. Sweet. But...

![](surprisingly.png)

If the designer of a system is surprised by it actually working, we have a long way to go.
<!-- Q: What was that control theory book? ... it's the engineer's role to reduce surprises. -->

If a 2 kilogram hunk of metal (with spinning deadly knives) is going to fly autonomously close to people, running code that I wrote, I kinda want to know how it's going to behave.

When the security guard asks "is it safe?", I need to be able to come with more than just "gee I don't know, it will work as long as the environment is similar to the training data". If they ask "what does similar mean?", I can only shrug my shoulders and say something about cost functions.

<!-- ml is like saying, well it got it right so far, therefore it will get it right in the future. -->

The seperation principle is like saying, hey, instead of trying to analyze the stability of the entire thing (pixels to pose), let's split it up into seperate problems that we can reason about individually more easily. We can prove stability for each subproblem, and then, if the seperation principle holds, be sure that the cascade of connecting these systems together is still stable.

<!--
Consider a cascade of three subcomponents A -> B -> C.
We have analyze the "stability" of these subcomponents individually
and have arrived at these three results:
    input_a in set X => output_a in set Y
    input_b in set Y => output_b in set Z
    input_c in set Z => output_c in set W
where (in the above example, kinda simplified)
    W = {Poses within some angle and translation error of the true pose}
    Z = {Rectified 2D lines, where atleast four belong to the grid and are similar within a threshold to a 1x1m square}
    Y = {Edges, where those belonging to the edges of the grid lines are sufficiently linear and sufficiently many}
    X = {1280x720x3x255 pixel image, where the grid color is sufficiently white, stands out against background, atleast four visible, etc.}
-->

(c.f. Fossen, page 6, it's really all about the cascaded structure.)

If we design these sub-estimators correctly, we can come up with a set of intuitive conditions for which the estimate is guaranteed to be close to the true value.

I say intuitive, because it's hard to make perfect guarantees. But a good step along the way is to make sure that a person can read the assumption, look at the environment, and decide in their head whether or not the assumption holds.

That was the case in Fossen's paper: the assumptions all had intuitive meanings, like the symmetric mass matrix.

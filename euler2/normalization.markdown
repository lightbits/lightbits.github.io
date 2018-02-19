Aside: Normalization
--------------------

While gimbal lock is not one of them, tracking orientation with a rotation matrix, rather than Euler angles, does not come without its own set of issues. In particular, you might be worried by this piece of code that 'accumulates' a rotation to the current one:

    R = euler(rx,ry,rz)*R

If you are an expert in floating point numbers you probably know that they are not perfect realizations of the real numbers: for example, `0.1+0.1` is not `0.2`, but `0.20000000298023224`.

Imperfections like these can be a concern when you deal with rotation matrices (or quaternions) over longer periods of time&sup1;, in the sense that repeatedly appending rotations will accumulate errors and cause the matrix (or quaternion) to stray from a valid rotation: the columns are no longer unit-length and pair-wise perpendicular, and objects appear slightly deformed after rotation.

<p style="color:#999;">
&sup1;*Time* in the literal sense too: even if you don't touch that rotation matrix, you might want to check up on it from time to time if your hardware is operating in high-radiation environments (like space or a nuclear reactor), because an occasional bit-flip is a real risk.
</p>

If you use quaternions you can renormalize it: compute the length and divide by it. The analog for rotation matrices is called orthogonalization. You can find many ways to orthogonalize a matrix, depending on if you want higher accuracy or simpler code. Here's a simple one, used in Peter Corke's [Robotics, Vision and Control](todo: link to website) toolbox:

Given a slightly messed-up rotation matrix `R` with columns `(x, y, z)`
1. Pick an axis that you assume is in the correct direction (say `x`)

    `x' = x`

2. Pick another axis (say `y`) and create a third axis perpendicular to those two

    `z' = cross(x', y)`

3. But, because `x` and `y` may not have been perpendicular, make another axis perpendicular to the first one and the one you just made

    `y' = cross(z', x')`

4. Finally, normalize each axis to unit-length.

The fixed rotation matrix is then formed by the columns `(x', y', z')`. Another simple one can be found in Direction Cosine Matrix IMU: Theory by William Premerlani and Paul Bizard (paywalled, but you can find the procedure reprinted on StackOverflow [here](todo:)).



$$
\begin{pmatrix}
    \cos(y)\cos(z) & \cos(z)\sin(x)\sin(y) - \cos(x)\sin(z) & \sin(x)\sin(z) + \cos(x)\cos(z)\sin(y) \\
    \cos(y)\sin(z) & \cos(x)\cos(z) + \sin(x)\sin(y)\sin(z) & \cos(x)\sin(y)\sin(z) - \cos(z)\sin(x) \\
      -\sin(y) &            \cos(y)\sin(x) &            \cos(x)\cos(y) \\
\end{pmatrix}
$$

$$
\begin{pmatrix}
     1  &    xy - z  &  xz + y \\
     z  &  xyz + 1  &  yz - x \\
    -y  &          x  &        1 \\
\end{pmatrix}
$$

$$
\begin{pmatrix}
     1 & -z &  y \\
     z &  1 & -x \\
    -y &  x &  1 \\
\end{pmatrix}
$$

$$
R = I + \sin(a) \text{skew}(r) + (1 - \cos(a)) \text{skew}(r)^2
$$

$$
R = I + \sin(|w|) \text{skew}({w\over|w|}) + (1-\cos(|w|)) \text{skew}({w\over|w|})^2
$$

$$
R = I + \text{skew}(w)
$$

$$
\text{skew}(x,y,z) = \begin{pmatrix}
 0  & -z  &  y \\
 z  &  0  & -x \\
-y  &  x  &  0 \\
\end{pmatrix}
$$

$$
R = \begin{pmatrix}
 1  & -z  &  y \\
 z  &  1  & -x \\
-y  &  x  &  1 \\
\end{pmatrix}
$$

$$
R = \begin{pmatrix}
    &     &    \\
 X  &  Y  &  Z \\
    &     &    \\
\end{pmatrix}
$$

$$
    R' = \text{euler}(\cdot) R =
    \begin{pmatrix}
        &     &    \\
     \text{euler}(\cdot) X  &  \text{euler}(\cdot) Y  &  \text{euler}(\cdot) Z \\
        &     &    \\
    \end{pmatrix}
$$

$$
I + \text{skew}(w)
$$

$$
R' = R + \begin{pmatrix}
    &     &    \\
 w \times X  &  w \times Y  &  w \times Z \\
    &     &    \\
\end{pmatrix}
$$

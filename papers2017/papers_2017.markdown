
# Papers I loved in 2017

** [Improvements to the Rescue Robot Quince Toward Future Indoor Surveillance Missions in the Fukushima Daiichi Nuclear Power Plant](https://link.springer.com/chapter/10.1007/978-3-642-40686-7_2) **
<br>
<span style="color:#999;">Tomoaki Yoshida, Keiji Nagatani, Satoshi Tadokoro, Takeshi Nishimura and Eiji Koyanagi</span>

![](papers2017-6.jpg)

On the third floor of the Fukushima Daiichi Nuclear Power Plant Unit 2 sits a tracked-wheel robot named Quince, permanently disconnected from humanity due to an accidental severing of the communication cable upon returning home. Even if any battery were left, its electronics have long since exceeded radiation exposure limits and are unlikely to work. Over six missions, a team of operators remote controlled Quince and used the robot's unique capabilities to climb stairs, open doors, measure water spill and radiation levels.

I don't think I've ever been as excited while reading a research paper - it's like a sci-fi story! Not one of those gloomy, dystopian sci-fi's that everyone seems to be writing today. This is a genuine success story, showing the benefit that robotics can actually bring (and has brought) to society. This work got some coverage on [news sites](https://spectrum.ieee.org/energy/nuclear/dismantling-fukushima-the-worlds-toughest-demolition-project), and one of the operators kept a [personal blog](https://spectrum.ieee.org/automaton/robotics/industrial-robots/fukushima-robot-operator-diaries) that vanished for mysterious reasons...


<br>
<br>

**[Past, Present, and Future of Simultaneous Localization And Mapping: Towards the Robust-Perception Age](https://arxiv.org/abs/1606.05830)**
<br>
<span style="color:#999;">Cesar Cadena, Luca Carlone, Henry Carrillo, Yasir Latif, Davide Scaramuzza, José Neira, Ian Reid, John J. Leonard</span>

I discovered this article while struggling to find a topic (any topic!) for my master thesis, and it's thanks to a single sentence in it that I went in the direction I did. Upon reading that sentence, a connection formed in my head between a five year old [hobby project](http://9bitscience.blogspot.no/2013/07/raymarching-distance-fields_14.html) and the SLAM problem, and this allowed me to come up with an approach angle that differentiated me in the pile of super-smart researchers already in this area.

Philip Guo has [vlogged](https://www.youtube.com/watch?v=lfOK5LV1uiw) about this: discovering intersections between your personal strengths and experiences, and your research domain, is important, because otherwise you are competing against people with similar experiences, having similar ideas and doing similar projects, and you will have a hard time differentiating. It also makes me think of [learnbbaticals](https://www.youtube.com/watch?v=gpuMBuH3Ang): learning topics just for the fun of learning, without expecting it to pay dividends later on.

Having read more surveys since then I have come to appreciate how well-written this is: it's a helpful summary of the state of SLAM, but instead of being paragraphs of "X et al. did such and such" organized into sections (which is more like a dishonest bibliography) the authors synthesize the pieces of information together, point out the core ideas connecting papers, and draw conclusions regarding limitations and opportunities: it's not just a collection of knowledge, but a [knowledge accelerator](https://www.youtube.com/watch?v=EjWOQvdRA58).

<br>
<br>

** [BigSUR: Large-scale Structured Urban Reconstruction](http://geometry.cs.ucl.ac.uk/projects/2017/bigsur/) **
<br>
<span style="color:#999;">Tom Kelly, John Femiani, Peter Wonka, Niloy J. Mitra</span>

![](papers2017-1.jpg)

I became aware of this paper at the [3DV 2017 conference](https://lightbits.github.io/3dv17/). What I got from this was the idea of aiming for **stylization over realism**: the goal of 3D scanning (and photogrammetry and stuff) is to capture reality as close as possible. But because technology is imperfect, the result has holes (because of reflections or noise) or weird bubbly surfaces (because of over-smoothing), so they're not very compelling to look at. What this paper does is it uses the 3D scan as input to *generate* a stylized model based on high-level rules (like flat walls, windows, doors, and so on).

Sure, it misses some details that weren't in the ruleset, but you get a hole-free and nice-looking model out. Not only that, but you also get semantic labels like, this is a window or this is a door, instead of just a gigantic 3D mesh. Naturally, this falls right into the [keenest interests](https://www.justinobeirne.com/google-maps-moat) of our Face/Goo/Soft overlords, but you can imagine (for example) architect companies using this for good too.

<br>
<br>

** [Casual 3D Photography](http://visual.cs.ucl.ac.uk/pubs/casual3d/) **
<br>
<span style="color:#999;">Peter Hedman, Suhib Alsisan, Richard Szeliski, Johannes Kopf</span>

![](papers2017-4.jpg)

There's been a lot of impressive progress in the area of SLAM and 3D scanning, with stuff like [DSO](https://www.youtube.com/watch?v=C6-xwSOOdqQ), [SVO](https://www.youtube.com/watch?v=hR8uq1RTUfA), [ORB-SLAM](https://www.youtube.com/watch?v=ufvPS5wJAx0), [maplab](https://www.youtube.com/watch?v=9Ta7w_cs1lU) and [BundleFusion](https://www.youtube.com/watch?v=keIirXrRb1k), but these all fall short when it comes to creating 3D models that **just look good**. So these four authors (three of which are at Facebook) decided to do a **bang solid job** of 3D scanning, prioritizing good-looking and plausible models, rather than necessarily an unbiased depiction of real life.

This ties into the whole stylization versus realism idea from the above paper, and there are more papers that go the same route (like [this one](http://graphics.stanford.edu/projects/3dlite/)) and try to apply high-level rules to visually improve the result of 3D scanning.

<br>
<br>

** [How2Sketch: Generating Easy-To-Follow Tutorials for Sketching 3D Objects](http://geometry.cs.ucl.ac.uk/projects/2017/how2sketch/) **
<br>
<span style="color:#999;">James W. Hennessey, Han Liu, Holger Winnemöller, Mira Dontcheva, Niloy J. Mitra</span>

![](papers2017-2.jpg)

The papers above led me to discover yet more awesome papers from University College London's [Smart Geometry Processing Group](http://geometry.cs.ucl.ac.uk/index.php) and [Adobe Research](https://research.adobe.com/projects/). This one in particular struck my fancy as, having done a few years of learning to draw myself (and not being particularly successful), the idea of combining computing with an analog hobby was eye-opening, and kept me awake thinking about ways to use machine learning, computer vision and computer graphics techniques, for learning to draw: like accelerating the creative process by [interactively generating designs](https://www.autodeskresearch.com/publications/dreamsketch-early-stage-3d-design-explorations-sketching-and-generative-design), teaching you [perspective](http://williforddesign.com/publications/).

<br>
<br>

** [CurveUps: Shaping Objects from Flat Plates with Tension-Actuated Curvature](http://visualcomputing.ist.ac.at/publications/2017/CurveUp/) **
<br>
<span style="color:#999;">Ruslan Guseinov, Eder Miguel, and Bernd Bickel</span>

![](papers2017-3.jpg)

In a similar vein to applying computational power to drawing, I enjoyed discovering that people are using computing to rethink physical fabrication as well: making [machine-knitting accessible to everyone](https://www.youtube.com/watch?v=iEaK68VRAng), and helping us design [joinery furniture](https://jiaxianyao.github.io/joinery/) (assembled solely by interlocking joints). This paper was particularly cute because I love the idea of printing a flat sheet of stuff and having it automatically morph itself into a 3D shape.

<br>
<br>

** [Polarimetric Multi-View Stereo](https://www.youtube.com/watch?v=UhwayReG9i8) **
<br>
<span style="color:#999;">Zhaopeng Cui, Jinwei Gu, Boxin Shi, Ping Tan, Jan Kautz</span>

![](papers2017-5.jpg)

This one has no particular ties with the other papers here, but I liked it because it made me think differently about sensors and algorithms for doing computer vision. It turns out that the polarization of light can tell us something about the orientation of the surface it came from, which is super useful in 3D reconstruction for resolving ambiguities.

This was information that was readily available to us from nature, that we could easily capture if we built our cameras to do so, but we were **throwing it away** because we thought it wasn't useful. It makes me wonder what else we are throwing away that could make our problems easier to solve, our algorithms more robust or efficient.

It's easy to take the sensors we already have - like RGB cameras, depth cameras and laser scanners - and think that's all we have got to work with, that this is all that nature is giving us to solve our problem. But maybe there are other physical phenomenon we can exploit, other weird properties of light, or radio waves, or quantom effects - who knows - that would drastically simplify the problem, if we build sensors to measure them.

<br>
<br>

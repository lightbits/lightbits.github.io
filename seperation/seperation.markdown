# Providing Guarantees in Robotic Vision Systems with the Seperation Principle

<style type="text/css">
body { font-size: 16px; text-align:justify; color: #000; }
h1,h2,h3 { margin: 0;}
h1 { border: none; font-size: 200%;}
h2 { font-size:180%; line-height:200%;}
h3 { font-size:100%; line-height:250%;}
</style>

> *The main task of the engineering analyst is not merely to obtain "solutions" but is rather to understand the dynamic behavior of the system in such a way that the secrets of the mechanism are revealed, and that if it is built it will have no surprises left for [them]. Other than exhaustive physical experimentation, this is the only sound basis for engineering design, and disregard of this cardinal principle has not infrequently led to disaster.*

> *From "Analysis of Nonlinear Control Systems" by Dustan Graham and Duane McRuer, 1964, p. 436.*

<span style="font-size:250%;font-weight:bold;float:left;margin-bottom:-6px;padding:0 12px 0 0;line-height:1.1;">
A</span> friend recently sent me a neat paper. The first-page figure showed a street occupied by a cyclist, obediently waiting on the light to turn green, foregrounded by a quadcopter, with an abundance of colorful lights indicating battery, flight controller status, and possibly even a hint of consciousness.

The figure caption was a concise summary: "Trained with data collected by cars and bicycles, the quadcopter has learned to follow basic traffic rules. Surprisingly, the policy learned is highly generalizable, and even allows for flight in indoor corridors and parking lots".

What made me worried as I read this is not that the system works (and end-to-end learning threatening to invalidate all the time I spent learning mathematical modeling)

... If you read the rest of the paper, they actually address this, and explain why it worked, when it works.

Is it unfair of me to say that our discipline has a major problem if the designer of a system is surprised when the system works? Strictly speaking, they didn't intend for the system to work indoors or in parking lots anyway; from their point of view, those were just happy accidents. But, as I had the dissatisfaction of coming to know in my overly ambituous time as an artist-in-training, eagerly following Bob Ross' scruffy hair and his pencil move decisively across the canvas, not all accidents are happy. In fact, most of my accidents were definitely not happy, and although my boyhood sketches could harm no one with their lack of taste, the same cannot be said for systems that can affect or damage people's lives; the most important quality of these systems is that they do not offer any surprises. If we want to apply machine learning, or even more "classical" computer vision algorithms, outside the lab, there cannot be any surprises.

![](out2.jpg)

The image above is from a past project I helped with. It shows me clumsily walking around with a custom-built quadcopter (with some outrageously expensive sensors that make your palms sweaty just looking at it) in order to "unit-test" a computer vision module. The module had one job: take the most recent image from our downward facing camera and spit out the quadcopter's position within a grid pattern on the floor.

A lot was resting on the success of this module, not just because everything else in the project built on top of it, but (maybe more importantly) because we wanted to be confident that we could "let the darn thing loose" in a room with people and not cause a disaster. It was a tricky problem because we couldn't make assumptions about what that room would look like. We didn't have photo references or specifications beyond "if a person is able to see a white, 1x1 meter tiled grid, your robot should too." We were told we could investigate the venue an hour before deploying the robot, but until then we had a year to build a system that we thought would work.

![](outs.jpg)

We did have some clues as to what the venue would look like (as we found out when trying to reserve a full-scale test arena, there's only so many indoor places with a 20x20 meter clean floor). So we tried to prepare for some probable-worst-case scenarios. Unfortunately our robot had some fairly high standards to meet, as humans are unsurprisingly very good at seeing patterns.

We prepared to face lots of nasty scenarios: a gym floor layered with sports markings where the grid is just taped on top; a low-contrast sheet of brown paper; or a glossy and noisy carpet. The possibility of both low- and high-frequency texture, as well as other confusing structures (like markings, spectators and safety nets), made it difficult to have *one* approach work in *all* cases. Eventually, we covered enough of them to be satisfied.

Of course, there were still many cases we couldn't cope with: for example, it failed when we flew too low, or when the sun shone through the window. But we knew ahead of time that it would fail in those cases, and we were also confident that it would work in the good cases before testing it (though of course, we would still test it). We could go to a physical place, or look at a photo, and almost guarantee that it would work or not. It didn't surprise us when it worked or when it failed.

That is *predictability*, and I want to argue that the field of computer vision should strive for predictability, much in the same way that the field of control theory strives for *stability*. To make my argument, I first need to explain what control theorists talk about when they talk about stability. <!-- it's a tool to understand the properties of the system; when it works, when it doesn't; how strong disturbances can be; how far it will be away from target. --> I'll then explain why stability is hard to achieve in computer vision, and why predictability is not only a more easily attainable fruit, but also provides most of the practical benefits of stability without the rigorous proofs.

<!--
### Predictability in computer vision

I don't want to give the impression that this is just a case of *those computer vision researchers being lazy and not doing the right thing*. The truth is that they care, but it's really hard to do. A major problem is just the complexity of the input, compared with the problems considered in control theory.

But there's something we can learn from them.

The attitude is different: it is not as expected to provide predictable computer vision algorithms, as it is to provide a stability proof for your controller. Cite futuremapping: "... don't work."

For generalization the approach taken seems to be akin to an abridged version of "will it blend?": let's try and see what happens!
-->


### Stability in control theory

Control theory is about making things do what you want. It came about during the second world war, when political and military powers wanted to make machines (and people) do what they want.

Here's an example: In the 70's, a renowned professor of control theory at my university decided to try controlling salmon using light and electrical signals to make them swim one way or another. According to rumours still echoing in the hallways, the project ended abruptly because some russian academics got a whiff of what was going on and expressed deep concerns that Norway was going to herd all the fish in the sea to the Norwegian coastline.

![](fish-barrier.png)
> From "Recent progress in the control of fish behaviour" by J.G. Balchen, 1984.

The real reason we don't have mind-controlled salmon today, I think, is a bit less political: it just didn't work. But theoretically&mdash;and I imagine said person used an argument along these lines to get permission to build a dedicated salmon mind-control lab&mdash;there's no reason why it can't work: if you have a model of how the fish reacts to a given electrical signal, say a 5 volt impulse on its left makes it turn 45 degrees to the left, and a 5 volt impulse on the right makes it turn to the right, then you can devise a series of timed electrical impulses that make it follow any desired path, to the degree that the fish is capable.

![](control-of-fish.png)
> Figure from "Recent progress in the control of fish behaviour" by J.G. Balchen, 1984. The box labelled "FISH" represents the mathematical model of the fish behaviour.

The problem, or one of them, is that he didn't have a good model of how the electrical signals affected the salmon's motion. Maybe he got a pretty good idea of one particular salmon, but when he applied the same model to another salmon it presented a completely different pattern or its reaction changed over time or in the presence of other fish. Either way, the model did not accurately describe reality; there was a modelling error. The second problem is that, even with a good model of the fish, he didn't have a good model its environments: unpredictable oceanic currents, surrounding fish, food and predators, will cause the fish to stray, either voluntarily or not, from the desired path to the Norwegian coastline.

<!-- "Why is control theory hard?" Because you don't know, and can't predict, everything, and also control saturation.  -->
Which brings us to the notion of stability: in control theory, stability is a mathematical statement saying that a system will do what you want, despite unknown disturbances or modelling errors. If your model is perfect and all disturbances are known ahead of time, you can apply control signals to exactly cancel the disturbances and make the system do exactly what you want. But, in practice, the model is often imperfect, and disturbances are unpredictable, so people are very much concerned about how big these disturbances and errors can be, yet still have the system do "almost" what you want. This concern is embedded in the field to such a degree that if you publish a paper in a control theory journal, proposing a controller for some system&mdash;maybe to control the position of a ship in bad weather [*](fossen1999) or to land a small airplane on a pole [*](todo: perch a plane)&mdash;it is expected that you give a proof of stability under realistic assumptions, if you want to be taken seriously.

To give you an idea of what stability can look like, let's look at a paper called *Funnel Libraries for Real-Time Robust Feedback Motion Planning*, by Anirudha Majumdar and Russ Tedrake. Majumdar and Tedrake's goal was to control a small airplane (about the size of your upper body) through environments with unpredictable disturbances, like wind or trees with uncertain positions, as well as modelling errors. Because these disturbances are unpredictable, they can't just plan a series of inputs ahead of time and run with it, expecting to avoid all the trees.

![](funnels-3.jpg)
<p style="margin-left:auto;margin-right:auto;width:75%;font-size:90%;">From *"Funnel Libraries for Real-Time Robust Feedback Motion Planning"*, by Anirudha Majumdar and Russ Tedrake.</p>

<!-- Regularly check where you end up, and decide what motion to execute for the next chunk of time. Within that chunk, you can reason exactly about what the plane might do for a given motion pattern, and choose the pattern that safely avoids the tree in all cases. -->
<!-- I am now here, there is the tree, I am going to plan what to do for the next (e.g.) 500 milliseconds. If I execute these inputs, I'll end up somewhere inside this funnel. Might not be in the exact center, because my model is inexact, and wind might affect me. -->
<!-- The stability here, is the funnel: it is a guarantee that the airplane will be so and so close to the planned path. This guarantee becomes a powerful prediction tool: could I hit a tree if I follow these inputs? And thereby a powerful decision-making tool. -->

What does that mean? Well, such a plan would contain the exact propeller speed, rudder angle, and so on, at each point in time, that the airplane should maintain. If you have a mathematical model of the airplane, you can predict how it's going to fly when executing that plan, giving you a "planned" path.

Of course, its actual path is going to be different because of stuff you didn't know ahead of time that can disturb the airplane during flight, like wind. It could also be that your model is inaccurate, maybe the airplane is much heavier than your model says. Either of these can make the airplane take a path significantly different from the one you planned.

They wanted to take this uncertainty into consideration so as to make better plans. Using the tools presented in the paper, they can compute not just how the airplane will move when executing a plan, but also the outer bounds of its motion; where it can potentially end up by a gust of wind. These bounds on the output&mdash;the path&mdash;are based on bounds on the input&mdash;in this case, unknown external forces. If we expect 10mps wind, then the bounds will be larger than if we expect 1mps wind.

A person&mdash;or in their case, an algorithm&mdash;can then reason about the safety in executing a plan, or, if necessary, look for a better one: e.g. if the airplane can possibly crash into a tree. This is a form of stability that gives us a guarantee that the airplane will successfully fly without crashing, or atleast, if it will crash, that we know it ahead of time. As the airplane goes along, it executes a plan, checks where it ended up, and then decides what plan to execute next, picking the safest one.

<!-- ... not quite? This is a real-time planning thing. The actual bounds of the path would grow much bigger. I think the idea with funnels is that you execute a funnel, and then check where you ended up, and compute a new funnel. -->
<!-- so maybe the figure doesn't show the entire bounded path as predicted from t0, but rather as predicted as the airplane flew? -->

<!-- The approach we take here is to assume that disturbances/uncertainty are bounded and provide explicit bounds on the reachable set to facilitate safe operation of the system. -->

<!-- ...not quite what the paper is about? -->
<!-- it's more about combining these funnel motion plans to generate one complete plan? -->
<!-- Using the tools in that paper, they can compute not just the plan of inputs, but also the *bounds* of the resulting path, taking into account potential disturbances, like wind gusts or even inaccuracies in the mathematical model of the airplane. -->

<!-- ![](funnels-1.jpg)
<p style="margin-left:auto;margin-right:auto;width:75%;font-size:90%;">Also from *"Funnel Libraries for Real-Time Robust Feedback Motion Planning"*, by Anirudha Majumdar and Russ Tedrake.</p> -->

<!-- what do you mean? this isn't perceiving the environment. this is just executing a plan of inputs. the resulting trajectory is what the proof says something about. -->
<!-- I don't think their intention was to consider perception uncertainty... This is a tool you can use to cope with perception uncertainty. e.g. look for paths with sufficient margin of error away from uncertain obstacles. -->
<!-- and they kinda do consider uncertainty in perception? in terms of bounds. But the problem is that we can't provide those bounds. -->
<!-- But although the guarantee includes uncertainty *in* the environment, it doesn't include uncertainty in *perceiving* the environment: they avoid that problem with a couple of high-speed cameras on the walls and special markers on each object, allowing them track the position of things down to the millimeter. -->

<!-- Quantifying these bounds involves a consideration of all possible inputs and disturbances at any point in time, and showing that, for bounded disturbances, the airplane stays within bounds of the planned path. -->

### Stability in computer vision?

We want something similar for computer vision: that if the algorithm is given an image "within certain bounds", the output will be close to the true answer, within a margin of error. If that margin of error is unacceptable, somewhere in the environment, then we can prepare to handle that.

![](wind.png)

<!-- ![](wind-image.png) -->

But the input to a computer vision algorithm is much more complex: it involves a consideration of all possible images the camera can produce, all permutations of colors caused by noise and static in the camera sensor, slight changes of the viewpoint or the lighting. Whereas an external force, like wind, has one dimension of variation (or three, if you consider it to be a vector), an image of 1920x1080 pixels has 6 million dimensions, 6 million different ways it can change.

![](image.png)

Not only would it take way impossibly

Quantifying stability for a computer vision algorithm is to consider all possible images the camera can produce and determine for which images it computes the right answer. Unfortunately, you can make a lot of different images with 1920x1080 pixels, and this is where things get hard for us humans.

For the airplane, you can make statements like "for external forces, such as wind gusts, that are within 1 Newton, the airplane will not deviate from the path by more than 1 meter." But what is the equivalent of "within 1 Newton" for an image?


<!-- Our input has a dimension of 6 million. -->

<!-- For example, I could prove a form of stability for the grid pattern detector -->
<!-- Part of proving stability is making assumptions about what the input can be. So a straightforward thing to do, if I wanted to prove stability, is to run it on these images and say: it worked, it's stable!  -->
<!-- unless you make very specific assumptions? e.g. the image has to be exactly <this> -->

<!-- and then there is the fact that your model might not even reflect the system that exists in reality! So why even bother with these proofs? -->
<!-- But let's consider *why* we prove stability in the first place? -->

<!-- But why do people prove stability? They make assumptions too. Ultimately, it's about predictability. It's about building a system that reduces surprises. It's about building a system that behaves in a way you expect when you deploy it. The stability proof is just an *aid* to achieve that. -->

<!-- Specifically that a *person* should be able to look at an input to the system, and, with reasonable knowledge the system, predict how it's going to behave. -->



### Breaking the problem into smaller problems

<!-- the funnels concept is similar... but for trajectories? -->

Here's the problem: our input is a 1080p HD RGB image. Let's pretend each color is a continuous value, and flatten this into a 1920x1080x3 vector, giving us a 6220800-dimensional input. Our output that we want to compute is a 2D position in the grid.

Somehow we need to go from 6 million numbers down to two numbers, and we want to verify that those two numbers are correct.

The machine learning approach is to learn a function that maps those 6 million numbers to two numbers directly, by feeding it a bunch of example photographs of grids, with the corresponding 2D position, and then hoping that it works outside of those examples&mdash;that it "generalizes".

But it's hard for a human to predict if it does work for images outside of those examples. It's hard to tell what "invariants" have been encoded in the neural network; what conditions have to be satisfied for it to compute the right answer.

So what does a predictable system look like?

The key to this is to use a *seperation principle*. It sounds advanced, but it's a technique you're already well-familiar with.

<!-- This concept is well known in control theory. Seperation Principle. Boston Dynamics Marc Raibert 7:38: Decompose control problem into many seperate controllers that operate in different regions of state space. That allowed us both to have programmers work on multiple solutions to the problem, and have the complexity of each controller simplified by only having to operate in a small part of the dynamic space. -->


<!-- You can still do this with neural networks. The difference is that, instead of me hand-crafting algorithms to convert to grayscale, extract edges and form lines, these transformations are neural networks.

However, a person still decided the intermediate inputs and outputs to be these intuitive quantities, instead of "2nd convolutional output, etc." - which are still designed by a human, but it's hard to ...?

There is some work being done on proving the stability of neural networks, which could become useful here. For example, you could prove that for a subset of possible "list of 20 lines" inputs, the answer is within certain bounds of the true answer.

I'm not sure if this is what we want. Would you be able to make a "interpretable" stability proof in the 20,000 edge input?

Ultimately, a person is going to use this to make predictions, so the conditions under which it computes the right answer should be intuitive? Or not? I guess you could do something like, walk around with the robot in the environment you want to deploy it, inspect the stability margins, and deem it to be safe or not...

I feel like there are still surprises there...
-->

### A seperation principle for robot vision

Can we apply this to perception? Yes!

* The grid is detected given atleast four line observations belonging to the grid that form a sufficiently 1x1 meter square.

* A line is detected if it has sufficiently many edge pixels in a sufficiently straight line.

* An edge pixel is detected if the magnitude of the image gradient at that pixel is sufficiently large.



### Explainability

For machine learning we want to do something similar, but in *reverse*.

Instead of going, "alright, let's design a system that will work if we fly high enough, and the sun doesn't shine, and the lines are sufficiently white", and then writing the code that works under those conditions, machine learning wants to take a bunch of data, train a neural network on it, and then ask, "alright neural network, **what conditions have to be true in order for you to compute the right answer?**"

* Figure out what properties it has "learned". What are the invariants under which it computes the right answer?
* Given an output, what are the possible input images that produce a value close to it?

Unfortunately, we are a far way off from generating explanations / conditions that a human can intuitively understand.

Intuitive explanations are necessary for us to *predict*.

<!-- visualizing-and-understanding -->

### Notions of similarity

Here's a turtle. We can all agree it's a turtle. Why? It has a shell, and four limbs that look like legs, and a head that sticks out, and it's shape is kinda turtle-like. It's a turtle.

Wrong! This is a gun. No matter how you look at it, it is definitely not a turtle. Atleast, according to this neural network.

This is not just some random crappy neural network that someone tried on their favorite turtle, and was disappointed when it didn't notice it as a turtle. The turtle has been adverserially modified, its texture contains a pattern designed to cause this specific neural network to recognize it as something else, even though, to you and I, it looks like a turtle.

Neural networks are clearly using a different notion of similarity than us.

The goal of neural networks and machine learning may not be to copy the human visual system.

Because we have our own flaws. [blue-dress].

<!--  -->
<!-- "This is fascinating stuff, as usual. But it begs a question. This sort of attack seems to modify a greater number of pixels than previous methods - certainly more pixels than the one pixel attacks in the last video! But surely, if an image is modified enough, you could argue that what you are looking at is no longer truly a cat?" -->

However, if we want predictable systems, they need to use notions of similarity that we can understand.

### Anthropomorphization

There is an interesting connection with the "anthropomorphization considered harmful" debate; humorously depicted in a meme which recently made the rounds in the scientific Twitter-sphere.

<a href="anthropomorphization.jpg">
    <img style="padding:0 1em 0 0;float:left;width:100px;" src="anthropomorphization.jpg">
</a>

<!-- ![](anthropomorphization.jpg) -->

The argument for (the cap dude) is that it serves to aid us in predicting the behaviour of a coexisting AI. While I agree with the intent, I disagree with how it's currently implemented: i.e. slapping the term "learning" on an algorithm (that we don't even know is related to how we learn) and calling it a day. I don't think that helps us predict its behaviour.
<!-- To actually be useful to us, anthropomorphization has to be more than a superficial renaming of things. -->

<!-- related: MIT AGI - Life 3.0 (Max Tegmark) -->




### What can we learn from control theory?

> There are substantial academic incentives to tackle the hardest research problems, such as developing methods to address adversarial
examples and providing provable guarantees for system properties and behaviors

> To what extent, in what circumstances, and for what types of architectures can formal verification be used to prove key properties of AI systems? Can other approaches be developed to achieve similar goals by different means?

This has been established practice in control theory for decades. Their tools of trade: break it down, use mathematical models with simplifying, thought realistic, assumptions.

e.g. even if you already have an exact mathematical model of the system (like a neural network) it may be unwieldy to use in a stability proof. But perhaps you can make a simpler model of it?

**Break it into parts:** Could we gain something by, instead of developing end-to-end models that take the image on one end and give the final answer on the other, breaking these models up into parts that we design? e.g. a grid filter, an edge detector, a line detector, a position estimator. Train each seperately, analyze behaviour seperately. This is established practice in control theory, and they deal with even lower dimensional systems; yet it is missing from machine learning? (or maybe I'm mistaken.)

### Related: Interpretability

> With the growing success of neural networks, there is a corresponding need to be able to explain their decisions&mdash;including building conﬁdence about how they will behave in the real-world, detecting model bias, and for scientiﬁc curiosity.

Explaining the output is one thing, but for that to be useful it has to enable us to predict their behaviour. Unfortunately, attribution techniques fall short, since neural networks are still at the stage where their behaviour goes completely opposite of our expectation:

for example, the turtle. Comparing the two turtles, we would see no reason that the network would misclassify the second one.

### Conclusion

To what extent should we seek to understand the algorithms we have designed&mdash;or rather discovered&mdash;like we may seek to understand the values and beliefs of another society, versus imposing those of our own to make them more familiar, more governable? Absent of an uprising resulting from algorithmic annexation, I think the answer is a bit of both. Pushing the design from either direction, purely from data, or purely from our own beliefs, when taken to an extreme, will on one end lead to fickle black boxes <!-- susceptible to unpredictable whims caused by imperceivable changes to their input --> that make seemingly inexplicable decisions, or it will lead to similarly inexplicable decisions as those we may readily observe in our fellow bipedal beings.


### Stability of neural networks

Stability of convnets have been studied by a number of researchers. Cisse et al. found that imposing invariance under additive perturbations increased robustness against adverserial attacks [*](M. Cisse, P. Bojanowski, E. Grave, Y. Dauphin, and N. Usunier. Parseval networks: Improving robustness to adversarial examples). However, this is far from using stability as an analysis tool as one uses it in control theory.

Heravi et al. [*](analyzing stability of convolutional neural
networks in the frequency domain) did a more in-depth analysis of such additive perturbations, by studying the problem in the frequency domain. Yay Fourier!

Bietti and Mairal [*](Invariance and Stability of Deep Convolutional Representations) study how *pooling operations* provide invariance against certain input perturbations, like translation or small deformations. Unfortunately, this is still far from being able to prove that a turtle is detected as a turtle, regardless of viewpoint.

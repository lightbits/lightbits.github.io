<!-- reading: ANALYZING STABILITY OF CONVOLUTIONAL NEURAL NETWORKS IN THE FREQUENCY DOMAIN.  -->
Let's look at a different example. To a person, this four-limbed creature, with its head poking out of something resembling a shell, is obviously a turtle. But with some clever adjustments to its color and texture, the authors of this paper were able to fool a neural network into misclassifying it as something completely different: a rifle. This slip is surprising to us, yet one could say it is entirely predictable; after all, the neural network is nothing more than a statistical inference machine, whose exact computations can be scrutinized to find out why it arrived at that conclusion. The problem lies in our ability, or lack thereof, to predict these mishaps without taking out the calculator.

Neural networks have gained a reputation of being somewhat fickle, atleast to us humans, and researchers have understood that, if we are to deploy these systems outside the lab, possibly putting people's lives in danger, we need to better understand why they arrive at the conclusions they do.

<!-- the building blocks of interpretability -->
> With the growing success of neural networks, there is a corresponding need to be able to explain their decisions&mdash;including building conﬁdence about how they will behave in the real-world, detecting model bias, and for scientific curiosity.

A body of work is therefore emerging on visualizing or explaining what the network has "learned", in the form of "semantic dictionaries".

> Semantic dictionaries are powerful not just because they move away from meaningless indices, but because they express a neural network’s learned abstractions with **canonical** examples.

![](file:///C:/Temp/pages/The%20Building%20Blocks%20of%20Interpretability_files/mixed4d.jpeg)

We can from this gleam some properties: there's a lot of green, brown and snout-like images, so in absence of these qualities, it may be unlikely that your dog is correctly classified.

Unfortunately, although these examples may be "canonical", I think that definition says more about the network, than it being what a human would associate with a canonical example: i.e. a tool for predicting behaviour beyond specific examples, or, generalizing. The "canonical dog snout" may be a characteristic that the network has learned to look for, and although we can easily visualize it, it is not a characteristic that I can easily identify myself, when looking at a dog. Thus, it fails to help me predict *when images are classified as a dog*.

<!-- break it up -->
Detecting all sorts of objects. Of course, neural networks were designed to tackle large-scale problems of this sort, where the intermediate representations are difficult to come up with. Instead of designing a dog detector, a cat detector, a person detector, a car detector, etc., we just create one big everything detector. This is great! It saves us a lot of effort. On the other hand, have we lost something in abandoning this hand-crafted approach? We still distinguish between character recognition and object recognition. Why aren't these part of the same network?

Trustworthiness.

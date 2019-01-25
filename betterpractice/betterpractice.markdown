# Better research practice

Leslie Lamport's "State the Problem Before Describing the Solution" [*](https://www.microsoft.com/en-us/research/publication/state-problem-describing-solution/) is often prescribed as a must-read for the budding (or perhaps even the well-established) researcher, and also one that should be re-read on a regular basis. I certainly agree with this, but I prefer A.E. Eiben and J.E. Smith's Introduction to Evolutionary Computing, chapter 9: "Working with Evolutionary Algorithms", which gives a practical example of how one actually goes about *stating the problem*.

To give a bit of context, evolutionary computing or EC is a field centered around using evolution, or evolution-inspired algorithms, to solve problems. The field may be slightly disorienting to the newcomer as, to ones surprise, there appears to be *very many* evolutionary algorithms, instead of just *the one that nature uses*. It turns out that defining, exactly, what nature does is rather difficult.

As a consequence, we have things like the Evolutionary Computation Bestiary [*](https://github.com/fcampelo/EC-Bestiary), acronyms like GP, GA, ES, NES, NEAT, CMA, CMA-ES, as well as a bunch of extensions like coevolution, novelty search or quality diversity, that lead to a combinatorial explosion of possible algorithms.

Many of these are tested on the same type of problem, like optimizing the Rosenbrock function, or solving a maze, and they all more-or-less manage to solve the problem. So it's unclear what one algorithm actually allows you to do, that another doesn't.

Here's what the authors suggest, in chapter **9.4.2 Better Practice**:

A better example of how to evaluate the behaviour of a new algorithm takes into account questions such as:

1. What type of problem am I trying to solve?

2. What would be a desirable property of an algorithm for this type of problem, for example: speed of finding good solutions, reliably locating good solutions, or occasional brilliance?

3. What methods currently exist for this problem, and why am I trying to make a new one, i.e., when do they not perform well?

After considering these issues, a particular problem type can be chosen, a careful set of experiments can be designed, and the necessary data to collect can be identified.

<!-- > * How relevant are these results, e.g., are the test functions typical of real-world problems, or important only from an academic perspective?

> * What would have happened if a different performance metric had been used, or if the runs had been ended sooner, or later?

> * What is the scope of claims about the superiority of the tricky GA?

> * Is there a property distinguishing the seven good and two bad functions?

> * Are these results generalisable? Alternatively, do some features of the tricky GA make it applicable for other specific problems, and if so which?

> * How sensitive are these results to changes in the algorithm’s parameters?

> * Are the performance differences as measured here statistically significant, or can they be just artifacts caused by random effects?
 -->

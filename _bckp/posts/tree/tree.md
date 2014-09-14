Inspired by the hype generated around juletide by [anvaka](https://github.com/anvaka/atree), I had a go at the tree thing myself.
Check it out [here](/posts/tree/).

The interesting bit is calculating the timestep required for the bells to move at a uniform rate.

    function calculateDt(t, offset, ds, scale, radius, frequency) {
        sinft = Math.sin(frequency * t - offset);
        cosft = Math.cos(frequency * t - offset);
        dx = radius * (sinft + frequency * t * cosft);
        dy = scale;
        dz = radius * (cosft - frequency * t * sinft);
        return ds / Math.sqrt(dx * dx + dy * dy + dz * dz);
      }

I specify a desired distance to travel along the time step, ``ds``, and this function calculates
how much time must pass in order for that to happen.
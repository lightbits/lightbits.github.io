var time = 0.0,
    yscreenoffset = 300,
    xscreenoffset = 260,
    xscreenscale = 360,
    yscreenscale = 360,
    ycamera = 1.3,
    zcamera = -2.5;

requestAnimationFrame = function(callback) {
  window.setTimeout(callback, 1000 / 24);
};

function run() 
{
  var ctx = document.getElementById('canvas').getContext('2d');

  animationLoop();

  function animationLoop() {
    renderFrame();
    time += 0.1;
    if (time >= 1.0)
      time -= 1.0;
    requestAnimationFrame(animationLoop);
  }

  function calculateDt(t, offset, ds, scale, radius, frequency) {
    sinft = Math.sin(frequency * t - offset);
    cosft = Math.cos(frequency * t - offset);
    dx = radius * (sinft + frequency * t * cosft);
    dy = scale;
    dz = radius * (cosft - frequency * t * sinft);
    return ds / Math.sqrt(dx * dx + dy * dy + dz * dz);
  }

  function renderFrame() {
    ctx.clearRect(0, 0, 500, 500);
    ctx.globalAlpha = 1.0;

    ds = 0.046;
    frequency = 128.0;
    circleRadius = 1.0;
    scale = 10.0;
    radius = 4.0;
    drawSpiral("#550000", circleRadius, frequency * 0.99, ds, scale, radius * 0.98, Math.PI * 0.02);
    drawSpiral("#005535", circleRadius, frequency * 0.99, ds, scale, radius * 0.98, Math.PI * 1.02);
    drawSpiral("#00eba5", circleRadius, frequency, ds, scale, radius, Math.PI);
    drawSpiral("#ff0000", circleRadius, frequency, ds, scale, radius, 0.0);
    drawBells("#ffff00", circleRadius, frequency, ds, scale, radius, 0.0);
  }

  function drawSpiral(color, circleRadius, frequency, ds, scale, radius, offset) {
    t = time * calculateDt(0, offset, ds, scale, radius, frequency);
    i = 0;
    while (i <= 256)
    {
      x = radius * t * Math.sin(frequency * t - offset);
      z = radius * t * Math.cos(frequency * t - offset);
      y = scale * t;

      if (z > 0.0)
        ctx.globalAlpha = Math.abs(Math.atan2(z, x) - Math.PI / 2.0) / (Math.PI * 2.0);
      else
        ctx.globalAlpha = 1.0;
      drawCircle([x, y, z], circleRadius, color);

      dt = calculateDt(t, offset, ds, scale, radius, frequency);
      t += dt;
      i += 1;
    }
  }

  function drawBells(color, circleRadius, frequency, ds, scale, radius, offset) {
    t = time * calculateDt(0, offset, ds, scale, radius, frequency);
    i = 0;
    circleRadius *= 1.5;
    while (i <= 256)
    {
      x = radius * t * Math.sin(frequency * t - offset);
      z = radius * t * Math.cos(frequency * t - offset) + 0.1 * Math.cos(t * 32.0);
      y = scale * t + 0.1 * Math.sin(t * 768.0);
      if (z > 0.0)
        ctx.globalAlpha = Math.abs(Math.atan2(z, x) - Math.PI / 2.0) / (Math.PI * 2.0);
      else
        ctx.globalAlpha = 1.0;
      drawCircle([x, y, z], circleRadius, color);

      dt = calculateDt(t, offset, ds, scale, radius, frequency);
      t += dt;
      i += 1;
    }
  }

  function drawCircle(p, r, color) {
    p = project(p);
    ctx.beginPath();
    ctx.arc(p[0], p[1], r, 0, 2 * Math.PI, false);
    ctx.fillStyle = color;
    ctx.fill();
    ctx.closePath();
  }

  function project(coord) {
    return [xscreenoffset+xscreenscale*(coord[0]/(coord[2]-zcamera)),
            yscreenoffset+yscreenscale*((coord[1]-ycamera)/(coord[2]-zcamera))]
  }
}
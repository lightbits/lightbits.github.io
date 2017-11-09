#version 150

uniform float aspect;
uniform float tan_fov_h;
uniform float lens_radius;
uniform float focal_distance;
uniform vec2 pixel_size;
uniform mat4 view;

in vec2 texel;
uniform float time;
uniform sampler2D tex_sky;
out vec4 out_color;

// Epsilon is in scene-coordinates, which is why smaller geometry looks rounder up close than big geometry

// Smaller epsilon -> less leaking
#define EPSILON 0.0001
#define STEPS 512

// #define EPSILON 0.001
// #define STEPS 256

// Should use a bit larger epsilon for this?
#define NORMAL_EPSILON 0.001

#define Z_NEAR 0.1
#define Z_FAR 100.0
#define PI_HALF 1.57079632679
#define PI 3.14159265359
#define TWO_PI 6.28318530718
#define TAU (2*PI)
#define PHI (sqrt(5)*0.5 + 0.5)

vec3 SampleSky(vec3 dir)
{
    float u = atan(dir.x, dir.z);
    float v = asin(dir.y);
    u /= TWO_PI;
    v /= PI;
    v *= -1.0;
    v += 0.5;
    u += 0.5;
    return vec3(1.0, 1.0, 1.0);
    // return texture(tex_sky, vec2(u, v)).rgb*1.5;
}

////////////////////////////////////////////////////////////////
//
//                           HG_SDF
//
//     GLSL LIBRARY FOR BUILDING SIGNED DISTANCE BOUNDS
//
//     version 2016-01-10
//
//     Check http://mercury.sexy/hg_sdf for updates
//     and usage examples. Send feedback to spheretracing@mercury.sexy.
//
//     Brought to you by MERCURY http://mercury.sexy
//
//
//
// Released as Creative Commons Attribution-NonCommercial (CC BY-NC)
//
////////////////////////////////////////////////////////////////

// Clamp to [0,1] - this operation is free under certain circumstances.
// For further information see
// http://www.humus.name/Articles/Persson_LowLevelThinking.pdf and
// http://www.humus.name/Articles/Persson_LowlevelShaderOptimization.pdf
#define saturate(x) clamp(x, 0, 1)

// Sign function that doesn't return 0
float sgn(float x) {
    return (x<0)?-1:1;
}

vec2 sgn(vec2 v) {
    return vec2((v.x<0)?-1:1, (v.y<0)?-1:1);
}

float square (float x) {
    return x*x;
}

vec2 square (vec2 x) {
    return x*x;
}

vec3 square (vec3 x) {
    return x*x;
}

float lengthSqr(vec3 x) {
    return dot(x, x);
}


// Maximum/minumum elements of a vector
float vmax(vec2 v) {
    return max(v.x, v.y);
}

float vmax(vec3 v) {
    return max(max(v.x, v.y), v.z);
}

float vmax(vec4 v) {
    return max(max(v.x, v.y), max(v.z, v.w));
}

float vmin(vec2 v) {
    return min(v.x, v.y);
}

float vmin(vec3 v) {
    return min(min(v.x, v.y), v.z);
}

float vmin(vec4 v) {
    return min(min(v.x, v.y), min(v.z, v.w));
}




////////////////////////////////////////////////////////////////
//
//             PRIMITIVE DISTANCE FUNCTIONS
//
////////////////////////////////////////////////////////////////
//
// Conventions:
//
// Everything that is a distance function is called fSomething.
// The first argument is always a point in 2 or 3-space called <p>.
// Unless otherwise noted, (if the object has an intrinsic "up"
// side or direction) the y axis is "up" and the object is
// centered at the origin.
//
////////////////////////////////////////////////////////////////

float fSphere(vec3 p, float r) {
    return length(p) - r;
}

// Plane with normal n (n is normalized) at some distance from the origin
float fPlane(vec3 p, vec3 n, float distanceFromOrigin) {
    return dot(p, n) + distanceFromOrigin;
}

// Cheap Box: distance to corners is overestimated
float fBoxCheap(vec3 p, vec3 b) { //cheap box
    return vmax(abs(p) - b);
}

// Box: correct distance to corners
float fBox(vec3 p, vec3 b) {
    vec3 d = abs(p) - b;
    return length(max(d, vec3(0))) + vmax(min(d, vec3(0)));
}

// Same as above, but in two dimensions (an endless box)
float fBox2Cheap(vec2 p, vec2 b) {
    return vmax(abs(p)-b);
}

float fBox2(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, vec2(0))) + vmax(min(d, vec2(0)));
}


// Endless "corner"
float fCorner (vec2 p) {
    return length(max(p, vec2(0))) + vmax(min(p, vec2(0)));
}

// Blobby ball object. You've probably seen it somewhere. This is not a correct distance bound, beware.
float fBlob(vec3 p) {
    p = abs(p);
    if (p.x < max(p.y, p.z)) p = p.yzx;
    if (p.x < max(p.y, p.z)) p = p.yzx;
    float b = max(max(max(
        dot(p, normalize(vec3(1, 1, 1))),
        dot(p.xz, normalize(vec2(PHI+1, 1)))),
        dot(p.yx, normalize(vec2(1, PHI)))),
        dot(p.xz, normalize(vec2(1, PHI))));
    float l = length(p);
    return l - 1.5 - 0.2 * (1.5 / 2)* cos(min(sqrt(1.01 - b / l)*(PI / 0.25), PI));
}

// Cylinder standing upright on the xz plane
float fCylinder(vec3 p, float r, float height) {
    float d = length(p.xz) - r;
    d = max(d, abs(p.y) - height);
    return d;
}

// Capsule: A Cylinder with round caps on both sides
float fCapsule(vec3 p, float r, float c) {
    return mix(length(p.xz) - r, length(vec3(p.x, abs(p.y) - c, p.z)) - r, step(c, abs(p.y)));
}

// Distance to line segment between <a> and <b>, used for fCapsule() version 2below
float fLineSegment(vec3 p, vec3 a, vec3 b) {
    vec3 ab = b - a;
    float t = saturate(dot(p - a, ab) / dot(ab, ab));
    return length((ab*t + a) - p);
}

// Capsule version 2: between two end points <a> and <b> with radius r
float fCapsule(vec3 p, vec3 a, vec3 b, float r) {
    return fLineSegment(p, a, b) - r;
}

// Torus in the XZ-plane
float fTorus(vec3 p, float smallRadius, float largeRadius) {
    return length(vec2(length(p.xz) - largeRadius, p.y)) - smallRadius;
}

// A circle line. Can also be used to make a torus by subtracting the smaller radius of the torus.
float fCircle(vec3 p, float r) {
    float l = length(p.xz) - r;
    return length(vec2(p.y, l));
}

// A circular disc with no thickness (i.e. a cylinder with no height).
// Subtract some value to make a flat disc with rounded edge.
float fDisc(vec3 p, float r) {
    float l = length(p.xz) - r;
    return l < 0 ? abs(p.y) : length(vec2(p.y, l));
}

// Hexagonal prism, circumcircle variant
float fHexagonCircumcircle(vec3 p, vec2 h) {
    vec3 q = abs(p);
    return max(q.y - h.y, max(q.x*sqrt(3)*0.5 + q.z*0.5, q.z) - h.x);
    //this is mathematically equivalent to this line, but less efficient:
    //return max(q.y - h.y, max(dot(vec2(cos(PI/3), sin(PI/3)), q.zx), q.z) - h.x);
}

// Hexagonal prism, incircle variant
float fHexagonIncircle(vec3 p, vec2 h) {
    return fHexagonCircumcircle(p, vec2(h.x*sqrt(3)*0.5, h.y));
}

float fTriPrism(vec3 p, float hx, float hy)
{
    vec3 q = abs(p);
    return max(q.z-hy,max(q.x*0.866025+p.y*0.5,-p.y)-hx*0.5);
}

// Cone with correct distances to tip and base circle. Y is up, 0 is in the middle of the base.
float fCone(vec3 p, float radius, float height) {
    vec2 q = vec2(length(p.xz), p.y);
    vec2 tip = q - vec2(0, height);
    vec2 mantleDir = normalize(vec2(height, radius));
    float mantle = dot(tip, mantleDir);
    float d = max(mantle, -q.y);
    float projected = dot(tip, vec2(mantleDir.y, -mantleDir.x));

    // distance to tip
    if ((q.y > height) && (projected < 0)) {
        d = max(d, length(tip));
    }

    // distance to base ring
    if ((q.x > radius) && (projected > length(vec2(height, radius)))) {
        d = max(d, length(q - vec2(radius, 0)));
    }
    return d;
}

//
// "Generalized Distance Functions" by Akleman and Chen.
// see the Paper at https://www.viz.tamu.edu/faculty/ergun/research/implicitmodeling/papers/sm99.pdf
//
// This set of constants is used to construct a large variety of geometric primitives.
// Indices are shifted by 1 compared to the paper because we start counting at Zero.
// Some of those are slow whenever a driver decides to not unroll the loop,
// which seems to happen for fIcosahedron und fTruncatedIcosahedron on nvidia 350.12 at least.
// Specialized implementations can well be faster in all cases.
//

const vec3 GDFVectors[19] = vec3[](
    normalize(vec3(1, 0, 0)),
    normalize(vec3(0, 1, 0)),
    normalize(vec3(0, 0, 1)),

    normalize(vec3(1, 1, 1 )),
    normalize(vec3(-1, 1, 1)),
    normalize(vec3(1, -1, 1)),
    normalize(vec3(1, 1, -1)),

    normalize(vec3(0, 1, PHI+1)),
    normalize(vec3(0, -1, PHI+1)),
    normalize(vec3(PHI+1, 0, 1)),
    normalize(vec3(-PHI-1, 0, 1)),
    normalize(vec3(1, PHI+1, 0)),
    normalize(vec3(-1, PHI+1, 0)),

    normalize(vec3(0, PHI, 1)),
    normalize(vec3(0, -PHI, 1)),
    normalize(vec3(1, 0, PHI)),
    normalize(vec3(-1, 0, PHI)),
    normalize(vec3(PHI, 1, 0)),
    normalize(vec3(-PHI, 1, 0))
);

// Version with variable exponent.
// This is slow and does not produce correct distances, but allows for bulging of objects.
float fGDF(vec3 p, float r, float e, int begin, int end) {
    float d = 0;
    for (int i = begin; i <= end; ++i)
        d += pow(abs(dot(p, GDFVectors[i])), e);
    return pow(d, 1/e) - r;
}

// Version with without exponent, creates objects with sharp edges and flat faces
float fGDF(vec3 p, float r, int begin, int end) {
    float d = 0;
    for (int i = begin; i <= end; ++i)
        d = max(d, abs(dot(p, GDFVectors[i])));
    return d - r;
}

// Primitives follow:

float fOctahedron(vec3 p, float r, float e) {
    return fGDF(p, r, e, 3, 6);
}

float fDodecahedron(vec3 p, float r, float e) {
    return fGDF(p, r, e, 13, 18);
}

float fIcosahedron(vec3 p, float r, float e) {
    return fGDF(p, r, e, 3, 12);
}

float fTruncatedOctahedron(vec3 p, float r, float e) {
    return fGDF(p, r, e, 0, 6);
}

float fTruncatedIcosahedron(vec3 p, float r, float e) {
    return fGDF(p, r, e, 3, 18);
}

float fOctahedron(vec3 p, float r) {
    return fGDF(p, r, 3, 6);
}

float fDodecahedron(vec3 p, float r) {
    return fGDF(p, r, 13, 18);
}

float fIcosahedron(vec3 p, float r) {
    return fGDF(p, r, 3, 12);
}

float fTruncatedOctahedron(vec3 p, float r) {
    return fGDF(p, r, 0, 6);
}

float fTruncatedIcosahedron(vec3 p, float r) {
    return fGDF(p, r, 3, 18);
}

// Rotate around a coordinate axis (i.e. in a plane perpendicular to that axis) by angle <a>.
// Read like this: R(p.xz, a) rotates "x towards z".
// This is fast if <a> is a compile-time constant and slower (but still practical) if not.
void pR(inout vec2 p, float a) {
    p = cos(a)*p + sin(a)*vec2(p.y, -p.x);
}

vec2 Rotate(vec2 p, float a) {
    return cos(a)*p + sin(a)*vec2(p.y, -p.x);
}

// Shortcut for 45-degrees rotation
void pR45(inout vec2 p) {
    p = (p + vec2(p.y, -p.x))*sqrt(0.5);
}

vec2 R45(vec2 p) {
    return (p + vec2(p.y, -p.x))*sqrt(0.5);
}

// Repeat space along one axis. Use like this to repeat along the x axis:
// <float cell = pMod1(p.x,5);> - using the return value is optional.
float pMod1(inout float p, float size) {
    float halfsize = size*0.5;
    float c = floor((p + halfsize)/size);
    p = mod(p + halfsize, size) - halfsize;
    return c;
}

// Same, but mirror every second cell so they match at the boundaries
float pModMirror1(inout float p, float size) {
    float halfsize = size*0.5;
    float c = floor((p + halfsize)/size);
    p = mod(p + halfsize,size) - halfsize;
    p *= mod(c, 2.0)*2 - 1;
    return c;
}

// Repeat the domain only in positive direction. Everything in the negative half-space is unchanged.
float pModSingle1(inout float p, float size) {
    float halfsize = size*0.5;
    float c = floor((p + halfsize)/size);
    if (p >= 0)
        p = mod(p + halfsize, size) - halfsize;
    return c;
}

// Repeat only a few times: from indices <start> to <stop> (similar to above, but more flexible)
float pModInterval1(inout float p, float size, float start, float stop) {
    float halfsize = size*0.5;
    float c = floor((p + halfsize)/size);
    p = mod(p+halfsize, size) - halfsize;
    if (c > stop) { //yes, this might not be the best thing numerically.
        p += size*(c - stop);
        c = stop;
    }
    if (c <start) {
        p += size*(c - start);
        c = start;
    }
    return c;
}

float ModInterval1(float p, float size, float start, float stop, out float c) {
    float halfsize = size*0.5;
    c = floor((p + halfsize)/size);
    p = mod(p+halfsize, size) - halfsize;
    if (c > stop) { //yes, this might not be the best thing numerically.
        p += size*(c - stop);
        c = stop;
    }
    if (c <start) {
        p += size*(c - start);
        c = start;
    }
    return p;
}

// Repeat around the origin by a fixed angle.
// For easier use, num of repetitions is use to specify the angle.
float pModPolar(inout vec2 p, float repetitions) {
    float angle = 2*PI/repetitions;
    float a = atan(p.y, p.x) + angle/2.;
    float r = length(p);
    float c = floor(a/angle);
    a = mod(a,angle) - angle/2.;
    p = vec2(cos(a), sin(a))*r;
    // For an odd number of repetitions, fix cell index of the cell in -x direction
    // (cell index would be e.g. -5 and 5 in the two halves of the cell):
    if (abs(c) >= (repetitions/2)) c = abs(c);
    return c;
}

// Repeat in two dimensions
vec2 pMod2(inout vec2 p, vec2 size) {
    vec2 c = floor((p + size*0.5)/size);
    p = mod(p + size*0.5,size) - size*0.5;
    return c;
}

// Same, but mirror every second cell so all boundaries match
vec2 pModMirror2(inout vec2 p, vec2 size) {
    vec2 halfsize = size*0.5;
    vec2 c = floor((p + halfsize)/size);
    p = mod(p + halfsize, size) - halfsize;
    p *= mod(c,vec2(2))*2 - vec2(1);
    return c;
}

// Same, but mirror every second cell at the diagonal as well
vec2 pModGrid2(inout vec2 p, vec2 size) {
    vec2 c = floor((p + size*0.5)/size);
    p = mod(p + size*0.5, size) - size*0.5;
    p *= mod(c,vec2(2))*2 - vec2(1);
    p -= size/2;
    if (p.x > p.y) p.xy = p.yx;
    return floor(c/2);
}

// Repeat in three dimensions
vec3 pMod3(inout vec3 p, vec3 size) {
    vec3 c = floor((p + size*0.5)/size);
    p = mod(p + size*0.5, size) - size*0.5;
    return c;
}

// Mirror at an axis-aligned plane which is at a specified distance <dist> from the origin.
float pMirror (inout float p, float dist) {
    float s = sgn(p);
    p = abs(p)-dist;
    return s;
}

// Mirror in both dimensions and at the diagonal, yielding one eighth of the space.
// translate by dist before mirroring.
vec2 pMirrorOctant (inout vec2 p, vec2 dist) {
    vec2 s = sgn(p);
    pMirror(p.x, dist.x);
    pMirror(p.y, dist.y);
    if (p.y > p.x)
        p.xy = p.yx;
    return s;
}

// Reflect space at a plane
float pReflect(inout vec3 p, vec3 planeNormal, float offset) {
    float t = dot(p, planeNormal)+offset;
    if (t < 0) {
        p = p - (2*t)*planeNormal;
    }
    return sgn(t);
}


// The "Chamfer" flavour makes a 45-degree chamfered edge (the diagonal of a square of size <r>):
float fOpUnionChamfer(float a, float b, float r) {
    return min(min(a, b), (a - r + b)*sqrt(0.5));
}

// Intersection has to deal with what is normally the inside of the resulting object
// when using union, which we normally don't care about too much. Thus, intersection
// implementations sometimes differ from union implementations.
float fOpIntersectionChamfer(float a, float b, float r) {
    return max(max(a, b), (a + r + b)*sqrt(0.5));
}

// Difference can be built from Intersection or Union:
float fOpDifferenceChamfer (float a, float b, float r) {
    return fOpIntersectionChamfer(a, -b, r);
}

// The "Round" variant uses a quarter-circle to join the two objects smoothly:
float fOpUnionRound(float a, float b, float r) {
    vec2 u = max(vec2(r - a,r - b), vec2(0));
    return max(r, min (a, b)) - length(u);
}

float fOpIntersectionRound(float a, float b, float r) {
    vec2 u = max(vec2(r + a,r + b), vec2(0));
    return min(-r, max (a, b)) + length(u);
}

float fOpDifferenceRound (float a, float b, float r) {
    return fOpIntersectionRound(a, -b, r);
}


// The "Columns" flavour makes n-1 circular columns at a 45 degree angle:
float fOpUnionColumns(float a, float b, float r, float n) {
    if ((a < r) && (b < r)) {
        vec2 p = vec2(a, b);
        float columnradius = r*sqrt(2)/((n-1)*2+sqrt(2));
        pR45(p);
        p.x -= sqrt(2)/2*r;
        p.x += columnradius*sqrt(2);
        if (mod(n,2) == 1) {
            p.y += columnradius;
        }
        // At this point, we have turned 45 degrees and moved at a point on the
        // diagonal that we want to place the columns on.
        // Now, repeat the domain along this direction and place a circle.
        pMod1(p.y, columnradius*2);
        float result = length(p) - columnradius;
        result = min(result, p.x);
        result = min(result, a);
        return min(result, b);
    } else {
        return min(a, b);
    }
}

float fOpDifferenceColumns(float a, float b, float r, float n) {
    a = -a;
    float m = min(a, b);
    //avoid the expensive computation where not needed (produces discontinuity though)
    if ((a < r) && (b < r)) {
        vec2 p = vec2(a, b);
        float columnradius = r*sqrt(2)/n/2.0;
        columnradius = r*sqrt(2)/((n-1)*2+sqrt(2));

        pR45(p);
        p.y += columnradius;
        p.x -= sqrt(2)/2*r;
        p.x += -columnradius*sqrt(2)/2;

        if (mod(n,2) == 1) {
            p.y += columnradius;
        }
        pMod1(p.y,columnradius*2);

        float result = -length(p) + columnradius;
        result = max(result, p.x);
        result = min(result, a);
        return -min(result, b);
    } else {
        return -m;
    }
}

float fOpIntersectionColumns(float a, float b, float r, float n) {
    return fOpDifferenceColumns(a,-b,r, n);
}

// The "Stairs" flavour produces n-1 steps of a staircase:
// much less stupid version by paniq
float fOpUnionStairs(float a, float b, float r, float n) {
    float s = r/n;
    float u = b-r;
    return min(min(a,b), 0.5 * (u + a + abs ((mod (u - a + s, 2 * s)) - s)));
}

// We can just call Union since stairs are symmetric.
float fOpIntersectionStairs(float a, float b, float r, float n) {
    return -fOpUnionStairs(-a, -b, r, n);
}

float fOpDifferenceStairs(float a, float b, float r, float n) {
    return -fOpUnionStairs(-a, b, r, n);
}


// Similar to fOpUnionRound, but more lipschitz-y at acute angles
// (and less so at 90 degrees). Useful when fudging around too much
// by MediaMolecule, from Alex Evans' siggraph slides
float fOpUnionSoft(float a, float b, float r) {
    float e = max(r - abs(a - b), 0);
    return min(a, b) - e*e*0.25/r;
}


// produces a cylindical pipe that runs along the intersection.
// No objects remain, only the pipe. This is not a boolean operator.
float fOpPipe(float a, float b, float r) {
    return length(vec2(a, b)) - r;
}

// first object gets a v-shaped engraving where it intersect the second
float fOpEngrave(float a, float b, float r) {
    return max(a, (a + r - abs(b))*sqrt(0.5));
}

// first object gets a capenter-style groove cut out
float fOpGroove(float a, float b, float ra, float rb) {
    return max(a, min(a + ra, rb - abs(b)));
}

// first object gets a capenter-style tongue attached
float fOpTongue(float a, float b, float ra, float rb) {
    return min(a, max(a - ra, abs(b) - rb));
}

float fPrism(vec3 p, vec3 b, float angle)
{
    vec3 q = p;
    q.x = abs(q.x);
    pR(q.xy, angle);
    return max(fBox(q, b), fBox(p - vec3(0,0.1,0), b));
}

vec2 ModPolar(vec2 p, float angle)
{
    float r = length(p);
    float t = atan(p.y, p.x);
    t = mod(t + angle/2.0, angle) - angle/2.0;
    p.x = cos(t)*r;
    p.y = sin(t)*r;
    return p;
}

vec2 Repeat(vec2 p, vec2 width)
{
    return mod(p+width/2.0, width) - width/2.0;
}

float fRoof(vec3 p, float width, float height, float depth, float angle)
{
    p.x = abs(p.x);
    p.xy = Rotate(p.xy, -angle);
    return p.y;
}

float fHouse1(vec3 p, float width, float height, float depth, float angle)
{
    float d = fBox(p, vec3(width, height, depth));
    p.y = p.y - height;
    p.x = abs(p.x);
    p.xy = Rotate(p.xy, -angle);
    float roof = fBox(p, vec3(width*1.4, 0.02, depth+0.02));
    d = max(d, p.y);
    d = min(d, roof);
    return d;
}

float fHouse2(vec3 p, float width, float height, float depth, float angle)
{
    float d = fBox(p, vec3(width, height, depth));
    p.y = p.y - height;
    p.x = abs(p.x);
    p.xy = Rotate(p.xy, -angle);
    float roof = fBox(p, vec3(width*1.4, 0.02, depth+0.2));
    d = max(d, p.y);
    d = min(d, roof);
    return d;
}

float Scene1(vec3 p)
{
    // bend around point
    #if 0
    float r = 0.3;
    vec3 q = p - vec3(-r,0,0);
    float t = atan(q.z,q.x);
    p.z = r*t;
    p.x = length(q.xz)-r;
    // return fCylinder(p.xzy, 0.1, 1.0);
    return fBox(p, vec3(0.1, 0.1, 0.3));
    #endif

    #if 0
    float r = 0.9;
    vec3 q = p - vec3(-r,0,0);
    float t = atan(q.z,q.x);
    p.z = r*t;
    p.x = length(q.xz)-r;

    p.xy = Rotate(p.xy, PI*4.0*p.z);
    return fBox(p, vec3(0.1, 0.1, 0.5))/4.0;
    #endif

    #if 1
    vec3 q = p;
    // q.xz = Rotate(q.xz, 1.5*q.x);
    q.z *= 1.0+0.8*p.x*p.x;
    return fBox(q, vec3(0.5,0.5,0.5));
    // return max(max(abs(p.x)-0.5, abs(p.y)-0.5), abs(p.z)-0.5);
    #endif
}

vec4 Scene(vec3 p)
{
    #if 0
    float k1 = 0.0;
    float k2 = 0.1;
    float k3 = 0.7;

    // float k1 = 0.1;
    // float k2 = 0.25;
    // float k3 = 0.7;

    // float body = fBox(p, vec3(0.3, k1, k2));
    // body = fOpUnionChamfer(body, fBox(p - vec3(0, -0.16, 0.1), vec3(0.3, 0.1, k3)), 0.2);
    float body = fBox(p, vec3(0.15, k1, k2));
    body = fOpUnionRound(body, fBox(p - vec3(0, -0.16, 0.1), vec3(0.3, 0.1, k3)), 0.7);
    body = max(body, fBox(p, vec3(0.25, 0.26, 0.8)));
    float d = body;
    {
        vec3 q = p;
        q.z -= 0.08;
        q.z = abs(q.z);
        q.x = abs(q.x);
        float hole = fCylinder(q.yxz - vec3(-0.25,0,0.42), 0.15, 0.5);
        float wheel = fCylinder(q.yxz - vec3(-0.25,0.2,0.42), 0.12, 0.05);
        d = max(d, -hole);
        d = min(d, wheel);
    }
    p.y += 0.38;
    #endif

    #if 0
    // float k1 = 0.05;
    // float k2 = 0.12;
    // float k3 = 0.6;

    float k1 = 0.2;
    float k2 = 0.4;
    float k3 = 0.5;

    // float k1 = 0.1;
    // float k2 = 0.25;
    // float k3 = 0.6;

    float body = fBox(p, vec3(0.15, k1, k2));
    // body = fOpUnionChamfer(body, fBox(p - vec3(0, -0.16, 0.2), vec3(0.3, 0.1, k3)), 0.2);
    body = fOpUnionSoft(body, fBox(p - vec3(0, -0.16, 0.2), vec3(0.3, 0.1, k3)), 0.8);
    body = max(body, fBox(p, vec3(0.25, 0.26, 0.8)));
    float d = body;
    {
        vec3 q = p;
        q.z -= 0.1;
        q.z = abs(q.z);
        q.x = abs(q.x);
        float hole = fCylinder(q.yxz - vec3(-0.25,0,0.35), 0.15, 0.5);
        float wheel = fCylinder(q.yxz - vec3(-0.25,0.2,0.35), 0.12, 0.05);
        d = max(d, -hole);
        d = min(d, wheel);
    }
    p.y += 0.38;
    #endif

    // _car1
    #if 0
    float body = fBox(p, vec3(0.15, 0.1, 0.2));
    body = fOpUnionSoft(body, fBox(p - vec3(0, -0.16, 0.2), vec3(0.3, 0.1, 0.6)), 0.8);
    body = max(body, fBox(p, vec3(0.25, 0.26, 0.8)));
    float d = body;
    p.y += 0.3;
    #endif

    // _car2
    #if 0
    float body = fBox(p, vec3(0.15, 0.2, 0.4));
    body = fOpUnionSoft(body, fBox(p - vec3(0, -0.16, 0.2), vec3(0.3, 0.1, 0.6)), 0.8);
    body = max(body, fBox(p, vec3(0.25, 0.26, 0.8)));
    float d = body;
    p.y += 0.3;
    #endif

    // _car3
    #if 0
    float body = fBox(p, vec3(0.15, 0.2, 0.4));
    body = fOpUnionSoft(body, fBox(p - vec3(0, -0.16, 0.2), vec3(0.3, 0.1, 0.5)), 0.8);
    body = max(body, fBox(p, vec3(0.25, 0.26, 0.8)));
    float d = body;
    p.y += 0.3;
    #endif

    // _car4
    #if 0
    float body = fBox(p, vec3(0.3, 0.1, 0.2));
    body = fOpUnionSoft(body, fBox(p - vec3(0, -0.16, 0.2), vec3(0.3, 0.1, 0.3)), 0.5);
    body = max(body, fBox(p, vec3(0.25, 0.26, 0.8)));
    float d = body;
    p.y += 0.3;
    #endif

    // _box
    #if 0
    float d = fBox(p, vec3(0.28));
    p.y += 0.3;
    #endif

    // _sphere
    #if 0
    float d = fSphere(p, 0.28);
    p.y += 0.3;
    #endif

    // _cylinder
    #if 0
    float d = fCylinder(p, 0.3, 0.28);
    p.y += 0.3;
    #endif

    // _torus
    #if 0
    float d = fTorus(p, 0.24, 0.6);
    p.y += 0.3;
    #endif

    // _prism
    #if 0
    float d = fTriPrism(p, 0.5, 0.6);
    p.y += 0.3;
    #endif

    // _cone
    #if 0
    vec3 q = p;
    q.y += 0.2;
    float d = fCone(q, 0.3, 0.8);
    p.y += 0.3;
    #endif

    // _plane
    #if 0
    // float d = max(p.z,max(p.y,p.x));
    float d = p.y;
    p.xz = mod(p.xz + vec2(1.0), vec2(2.0)) - vec2(1.0);
    d = max(d, -fCylinder(p, 0.5, 0.2));
    p.y += 20.0;
    #endif

    // _union
    #if 0
    float d = fBox(p, vec3(0.4,0.02,0.4));
    d = min(d, fSphere(p, 0.28));

    // float d = min(fSphere(p, 0.15), fCylinder(p, 0.05, 0.25));
    // d = min(d, fCylinder(p.yxz, 0.05, 0.25));
    // d = min(d, fCylinder(p.xzy, 0.05, 0.25));
    // // float d = min(fBox(p - vec3(0,-0.26,0), vec3(0.5,0.01,0.5)), fCylinder(p, 0.2, 0.25));
    p.y += 0.3;
    #endif

    // _intersect
    #if 0
    float d = fBox(p, vec3(0.4,0.02,0.4));
    d = max(d, fSphere(p, 0.28));

    // float d = max(fBox(p, vec3(0.5,0.2,0.5)), -fCylinder(p, 0.2, 0.25));
    p.y += 0.3;
    #endif

    // _subtract
    #if 0
    float d = fBox(p, vec3(0.4,0.02,0.4));
    d = max(d, -fSphere(p, 0.28));

    // float d = max(fBox(p, vec3(0.5,0.2,0.5)), -fCylinder(p, 0.2, 0.25));
    p.y += 0.3;
    #endif

    // _repeat
    #if 0
    vec3 q = p;
    q.x = mod(q.x + 0.5, 1.0) - 0.5;
    // q.z = mod(q.z + 0.7, 1.4) - 0.7;
    float d = fBox(q, vec3(0.4,0.02,0.4));
    d = max(d, -fSphere(q, 0.28));
    // d = max(d, fBox(p, vec3(1.5)));
    p.y += 0.1;
    #endif

    #if 0
    vec3 q = p;
    float r = 0.05;
    // q.y -= r/2.0;
    q.x = mod(q.x + r/2.0, r) - r/2.0;
    // q.x -= r/2.0;
    q.y = mod(q.y + r/2.0, r) - r/2.0;
    q.z = mod(q.z + r/2.0, r) - r/2.0;
    p.y -= 0.015/2.0;
    p.x -= 0.015/2.0;
    p.z -= 0.015/2.0;
    float d = fBox(p, vec3(0.2));
    d = max(d, -fSphere(q, 0.015));
    // d = max(d, fBox(p, vec3(1.5)));
    p.y += 0.22;
    #endif

    // _rotate
    #if 0
    vec3 q = p;
    q.xy = Rotate(q.xy, -0.5);
    float d = fBox(q, vec3(0.4,0.02,0.4));
    d = max(d, -fSphere(q, 0.28));
    p.y += 0.2;
    #endif

    // _mirror
    #if 0
    vec3 q = p;
    q.x = -abs(q.x);
    q.x -= 0.1;
    q.y = abs(q.y);
    // q.y -= 0.1;
    q.xy = Rotate(q.xy, -0.5);
    float d = fBox(q, vec3(0.4,0.02,0.4));
    d = max(d, -fSphere(q, 0.28));
    p.y += 0.32;
    #endif

    // _polar
    #if 0
    vec3 q = p;
    q.xz = ModPolar(q.xz, PI/1.0);
    q.x -= 0.5;
    float d = fBox(q, vec3(0.4,0.02,0.4));
    d = max(d, -fSphere(q, 0.28));
    p.y += 0.1;
    #endif

    // _chamfering
    #if 0
    float d = fBox(p, vec3(0.4,0.02,0.4));
    p.y -= 0.1;
    d = fOpUnionChamfer(d, fBox(p, vec3(0.1)), 0.1);
    // d = fOpUnionSoft(d, fBox(p, vec3(0.1)), 0.1);
    // d = fOpUnionRound(d, fBox(p, vec3(0.1)), 0.2);
    // d = min(d, fBox(p, vec3(0.1)));
    p.y += 0.2;
    #endif

    // _cup
    #if 0
    vec3 q = p;
    q.y *= -1.0;
    q.y += 0.6;
    float d = fCone(q, 0.5, 2.5);
    d = max(d, -fCone(q - vec3(0,-0.2,0), 0.45, 2.5));
    p.y += 0.3;
    #endif

    // _house
    #if 0
    vec3 q = p;
    float d = fBox(q, vec3(0.28, 0.28, 1.0));
    q.y -= 0.3;
    float roof = fPrism(q, vec3(0.28, 0.18, 1.0), -0.5);
    // roof = max(roof, fBox(p - vec3(0,0.3,0), vec3(0.28,0.28,0.5)));

    q = p;
    q.y -= 0.16;
    q.xz = q.zx;
    q.x -= 0.24;
    q.x = mod(q.x+0.24,0.48)-0.24;
    roof = min(roof, fPrism(q, vec3(0.28,0.28,0.25), -0.5));
    roof = max(roof, fBox(p, vec3(0.28,0.5,1.0)));

    d = min(d, roof);
    p.y += 0.3;
    #endif

    // _stockholm1
    #if 0
    float radius = 0.6;
    float leg_radius = 0.02;
    float leg_height = 0.4;
    float leg_offset = 0.9*radius;
    float thickness = 0.01;
    float support = 5.0*thickness;
    float d = fCylinder(p, radius, thickness);
    {
        vec3 q = p;
        q.xz = ModPolar(q.xz, PI/2.0);
        float leg = fBox(q - vec3(0,-support,0), vec3(leg_offset-leg_radius, support, leg_radius));
        q.x -= leg_offset;
        q.y += leg_height/2.0;
        q.xy = Rotate(q.xy, 0.15);
        leg = min(leg, fBox(q, vec3(leg_radius, leg_height/2.0, leg_radius)));
        leg = max(leg, -p.y - leg_height/1.1);
        d = min(d, leg);
    }
    p.y += 0.38;
    #endif

    // _stockholm2
    #if 0
    float radius = 0.6;
    float leg_radius = 0.02;
    float leg_height = 0.4;
    float leg_offset = 0.9*radius;
    float thickness = 0.01;
    float support = 5.0*thickness;
    float blend = 0.4;

    float d = fCylinder(p - vec3(blend*radius,0,0), radius, thickness);
    d = max(d, fCylinder(p - vec3(-blend*radius,0,0), radius, thickness));

    // legs
    {
        vec3 q = p;
        q.z = abs(q.z);
        q.z -= 0.4*radius;
        q.x = abs(q.x);
        q.x -= 0.3*radius;
        q.y += leg_height;
        q.xy = Rotate(q.xy, 0.15);
        q.zy = Rotate(q.zy, 0.1);
        float leg = fCylinder(q, leg_radius, leg_height);
        leg = max(leg, -p.y - leg_height);
        d = min(d, leg);
    }

    p.y += 0.4;
    #endif

    // _lack
    #if 0
    float width = 0.5;
    float depth = 0.25;
    float thick = 0.02;
    float height = 0.2;

    float d = fBox(p, vec3(width, thick, depth));

    // legs
    {
        vec3 q = p;
        q.xz = abs(q.xz);
        q.x -= width-thick;
        q.z -= depth-thick;
        q.y += height/2.0+thick;
        d = min(d, fBox(q, vec3(thick,height/2.0,thick)));
    }

    // undertable
    {
        vec3 q = p;
        q.y += height*0.8;
        d = min(d, fBox(q, vec3(width, 0.1*thick, depth)));
    }

    p.y += height+0.025;
    #endif

    // _boksel
    #if 0
    float width = 0.5;
    float depth = 0.5;
    float depth2 = 0.3;
    float thick = 0.02;
    float height = 0.2;
    float height2 = 0.1;
    float gap = 0.01;

    float d = fBox(p, vec3(width, height, depth));

    {
        vec3 q = p;
        q.x = abs(q.x);
        q.x -= width/2.0;
        q.z -= depth-depth2/2.0;
        d = max(d, -fBox(q, vec3(width/2.0-thick,height-thick,depth2)));
    }

    {
        vec3 q = p;
        q.x += width/2.0;
        q.y -= height/2.0 - gap;
        d = min(d, fBox(q, vec3(width/2.0-thick-gap,height2-thick,depth)));
    }

    p.y += height+0.025;
    #endif

    // _lappland
    #if 0
    float width = 1.25;
    float thick = 0.015;
    float width1 = width/5.0 - thick;
    float height1 = width1;
    float depth = 0.2;
    float height = 1.0;
    float gap = 0.01;

    float d = fBox(p, vec3(width/2.0, height/2.0, depth/2.0));

    float left = p.x - (-width/2.0 + width/5.0);
    float down = p.y - (-height/2.0 + width/5.0);
    float upper_right = max(-left,-down);
    upper_right = max(upper_right, p.x - width/2.0 + thick);
    upper_right = max(upper_right, p.y - height/2.0 + thick);

    // small boxes
    {
        vec3 q = p;
        q.y -= width/2.0;
        q.xy = Repeat(q.xy, vec2(width/5.0));
        float boxes = fBox(q, vec3(width1/2.0, height1/2.0, 2.0*depth));
        boxes = max(boxes, min(left,down));
        d = max(d, -boxes);
        d = max(d, -upper_right);
    }

    {
        vec3 q = p;
        q.y += height*0.15;
        float ledge = fBox(q, vec3(width/2.0, thick/2.0, depth/2.0));
        ledge = max(ledge, upper_right);
        d = min(d, ledge);
    }

    {
        // backwall
        float height2 = 0.7*height;
        vec3 q = p;
        q.z += depth/2.0;
        q.z -= thick;
        q.y -= height/2.0 - height2/2.0;
        q.x -= width/8.0;
        d = min(d, fBox(q, vec3(width1/2.0, height2/2.0, thick)));
    }

    p.y += height/2.0+0.025;
    #endif

    // _facade
    #if 0
    float depth = 0.01;
    float height = 0.48;
    float wall_width = 0.5;
    float plank_width = wall_width/20.0;
    float thick_depth = 5.0*depth;
    float window_width = wall_width*0.25;
    float window_depth = 4.0*depth;
    float window_height = wall_width*0.4;
    float spokes_radius = 0.3*depth;
    float spokes_gap = window_height/3.0;

    float left = p.x - 3.0*wall_width;
    float right = -p.x - 3.0*wall_width;

    p.x = mod(p.x + wall_width, 2.0*wall_width) - wall_width;

    vec3 q = p;
    float d = fBox(q, vec3(wall_width, height, depth));

    // large planks
    if (true)
    {
        q = p;
        q.x = abs(q.x);
        q.z -= thick_depth*0.5;
        d = min(d, fBox(q - vec3(wall_width,0,0), vec3(plank_width, height, thick_depth)));
    }

    // small planks
    if (true)
    {
        q = p;
        q.x = mod(q.x+plank_width, 2.0*plank_width)-plank_width;
        q.z -= depth;
        float d1 = fBox(q, vec3(plank_width/2.0, height, depth));
        d1 = max(d1, p.x-wall_width);
        d1 = max(d1, -p.x-wall_width);
        d = min(d, d1);
    }

    // window
    {
        q = p;
        d = max(d, -fBox(q, vec3(window_width, window_height, window_depth)));
    }

    // window spokes
    {
        q = p;
        float limit = max(q.y - window_height, -q.y - window_height);
        q.y = mod(q.y + spokes_gap, 2.0*spokes_gap) - spokes_gap;
        float spokes = fBox(q, vec3(window_height, spokes_radius, spokes_radius));
        spokes = min(spokes, fBox(q, vec3(spokes_radius, window_height, spokes_radius)));
        spokes = max(spokes, limit);
        d = min(d, spokes);
    }

    d = max(d, left);
    d = max(d, right);

    p.y += 0.3;
    #endif

    // _cross_shaft
    #if 0
    float height = 0.1;
    float radius_inner = 0.15;
    float radius_outer = 0.2;
    float radius_notch = 0.01;
    float notch_angle = PI/16.0;
    float shaft_length = 0.12;
    float shaft_radius = 0.05;

    // body
    float d = fCylinder(p, radius_outer, height);
    d = max(d, -fCylinder(p, radius_inner, height*2.0));

    // notches
    vec3 q = p;
    q.xz = ModPolar(q.xz, notch_angle);
    q.x -= radius_inner;
    // q.xz = R45(q.xz);
    // d = min(d, fBox(q, vec3(0.8*radius_notch, height, 0.8*radius_notch)));
    d = min(d, fCylinder(q, radius_notch, height));

    // repeat 90 degree
    q = p;
    q.xz = ModPolar(q.xz, PI/2.0);

    // bevel
    q.x -= radius_outer;
    d = fOpUnionChamfer(d, fCylinder(q.yxz, 0.06, 0.01), 0.03);

    // shaft
    q.x -= shaft_length;
    d = min(d, fCylinder(q.yxz, shaft_radius, shaft_length));
    d = max(d, -fCylinder(q.yxz, 0.25*shaft_radius, shaft_length*1.1));
    q.x += shaft_length/2.0;
    d = min(d, fCylinder(q.yxz, 1.1*shaft_radius, shaft_length/2.0));
    q.x += shaft_length/2.0;

    p.y += 0.11;
    #endif

    // _tless 02
    #if 0
    float height = 0.15;
    float radius = 0.2;
    float height2 = 0.4*height;
    float radius2 = 0.7*radius;
    float height3 = height;
    float radius3 = 0.5*radius;
    vec3 p0 = p;
    float d = fCylinder(p, radius, height);
    {
        vec3 q = p;
        d = max(d, fCone(q, radius, 1.0));
        // d = max(d, fSphere(p, 1.15*radius));
        d = min(d, fCylinder(p+vec3(0,0.5*height,0), radius, 0.5*height));
    }
    {
        vec3 q = p;
        q.y -= 0.8*height;
        float hole = fCylinder(q, 0.45*radius, 0.6*height);
        q.y -= 0.7*height;
        hole = max(hole, fSphere(q, 0.8*radius));
        // d = fOpDifferenceRound(d, hole, 0.015);
        d = max(d, -hole);
    }
    p.y += height + height2;
    d = min(d, fCylinder(p, radius2, height2));
    p.y += height2;
    // bottom cone
    {
        vec3 q = p;
        q.y *= -1;
        d = min(d, fCone(q, radius3, 1.5));
        d = max(d, -(p.y+height3));
    }
    // notches
    {
        vec3 q = p0;
        q.y -= height;
        q.xz = ModPolar(q.xz, PI/3.0);
        q.x -= radius;
        // d = fOpDifferenceRound(d, fSphere(q, radius*0.35), 0.015);
        d = max(d, -fSphere(q, radius*0.4));
    }
    p.y += 0.2;
    #endif

    // _tless 23
    #if 0
    float radius = 0.2;
    float height = 0.15;
    float radius_pin = 0.08*radius;

    vec3 q = p; float c;
    q.x = ModInterval1(q.x, 1.8*radius, -1, +1, c);

    // body cylinder
    float d = fCylinder(q, radius, height);

    // smooth edges
    d = max(d, fSphere(q, 1.6*height));

    // box junction
    d = min(d, fBox(p, vec3(radius*2.0, height, 0.7*radius)));

    // hollow cylinder
    d = max(d, -fCylinder(q-vec3(0,height,0), 0.8*radius, height));

    // inner pins
    {
        q = p;
        q.x = ModInterval1(q.x, 1.8*radius, -1, +1, c);
        if (c == 0) q.xz = Rotate(q.xz, 2.0);
        if (c == 1) q.xz = Rotate(q.xz, PI);
        float pin1 = fCylinder(q-vec3(radius*0.4,0,0), radius_pin, height);
        float pin2 = fCylinder(q-vec3(-0.1*radius,0,radius*0.4), radius_pin, 0.5*height);
        float pin3 = fCylinder(q-vec3(-0.1*radius,0,-radius*0.4), radius_pin, 0.5*height);
        d = min(d, pin1);
        d = max(d, -pin2);
        d = max(d, -pin3);
    }
    p.y += 1.5*height;
    d = min(d, fCylinder(p, 0.5*radius, height*0.5));
    p.y += height*0.5;
    {
        q = p;
        q.x = abs(q.x);
        q.x -= radius*0.5*0.4;
        d = min(d, fCylinder(q, 0.05*radius, height*0.5));
    }
    p.y += 0.1;
    #endif

    // http://www.blink-hus.no/blinkhus/hustype/tradisjonelle-hus
    // _house_2
    #if 0
    float d = fHouse1(p, 0.5, 0.7, 1.0, 0.55);
    vec3 q = p;
    q.xz = Rotate(q.xz, PI/2.0);
    q.y += 0.05;
    q.z += 0.4;
    d = min(d, fHouse1(q, 0.3, 0.7, 0.35, 0.45));
    d = max(d, -(p.y+0.7));
    p.y += 0.7;
    #endif

    // _house_3
    #if 0
    float d = fHouse1(p, 0.5, 0.7, 1.0, 0.55);
    vec3 q = p;
    q.xz = Rotate(q.xz, PI/2.0);
    q.x += 0.2;
    q.y += 0.11;
    q.z += 0.6;
    d = min(d, fHouse1(q, 0.35, 0.7, 0.5, 0.55));
    d = max(d, -(p.y+0.7));
    p.y += 0.7;
    #endif

    // _house4
    #if 0
    float c;
    float d = fBox(p, vec3(0.3, 2.0, 1.0));
    {
        vec3 q = p;
        q.y -= 1.0;
        float roof = fRoof(q, 0.5, 0.5, 0.5, 0.7);
        d = max(d, roof);
    }
    {
        vec3 q = p;
        q.x -= 0.3;
        q.y -= 0.4;
        q.y = ModInterval1(q.y, 0.2, -1, +1, c);
        q.z = ModInterval1(q.z, 0.6, -1, +1, c);
        q.z -= 0.1;
        d = min(d, fBox(q, vec3(0.1, 0.04, 0.08)));
    }
    d = max(d, -(p.y+0.1));
    p.y += 0.1;
    #endif

    #if 0
    vec3 q = p;
    q.xz = Rotate(q.xz, PI/2.0);
    q.z += 0.7;
    q.x += 0.4;
    float side = max(fHouse1(p, 0.5, 0.7, 1.0, 0.55), fHouse1(q, 0.5, 0.7, 0.7, 0.75));
    p.z -= 0.13;
    float d = fHouse1(p, 0.5, 0.7, 0.5, 0.55);
    d = min(d, side);
    d = min(d, fHouse1(q, 0.5, 0.7, 0.7, 0.75));
    d = max(d, -(p.y+0.7));
    p.y += 0.7;
    #endif

    // http://www.blink-hus.no/blinkhus/hustype/herskapelige-hus/hvalstad
    // _house_hvalstad
    #if 0
    float angle = 0.55;
    float width = 0.5;
    float height = 0.35;
    float depth = 1.0;
    float d = fBox(p, vec3(width, height, depth));
    {
        vec3 q = p;
        q.y = q.y - height;
        q.x = abs(q.x);
        q.xy = Rotate(q.xy, -angle);
        d = max(d, q.y);
    }
    {
        vec3 q = p;
        q.y = q.y - height;
        q.z = abs(q.z);
        q.z -= 0.5;
        q.zy = Rotate(q.zy, -angle);
        d = max(d, q.y);
    }
    {
        vec3 q = p;
        q.y += 0.25;
        float height2 = height*0.8;
        float wall = fBox(q, vec3(width+0.1, height2, depth+0.1));
        wall = max(wall, -fBox(q, vec3(width-0.05, height2+0.2, depth-0.05)));
        d = max(d, -wall);
    }
    {
        vec3 q = p;
        q.z = abs(q.z);
        q.z -= 0.23;
        q.y += 0.05;
        q.x -= 0.2;
        q.xz = Rotate(q.xz, PI/2.0);
        float top = fHouse1(q, 0.15, height, 0.2, 0.55);
        d = min(d, top);
        d = max(d, -(p.y+height));
    }
    {
        vec3 q = p;
        q.y += 0.45;
        q.x -= 0.43;
        d = max(d, -fBox(q, vec3(0.2, 0.4, 0.4)));
        q.z = abs(q.z);
        q.z -= 0.15;
        d = min(d, fBox(q, vec3(0.01, 0.4, 0.01)));
    }
    p.y += height;
    #endif

    // deformation
    #if 0
    vec3 dx = vec3(0.001, 0, 0);
    vec3 dy = vec3(0.0, 0.001, 0);
    vec3 dz = vec3(0.0, 0, 0.001);
    float dfdx = (Scene1(p + dx)-Scene1(p - dx))/0.002;
    float dfdy = (Scene1(p + dy)-Scene1(p - dy))/0.002;
    float dfdz = (Scene1(p + dz)-Scene1(p - dz))/0.002;
    float gradf = sqrt(dfdx*dfdx + dfdy*dfdy + dfdz*dfdz);
    float d = Scene1(p);
    p.y += 0.5;
    #endif

    // recovery shapes
    #if 0
    float d;

    // {
    //     d = fBox(p, vec3(0.5));
    //     p.y += 0.55;
    // }

    {
        vec3 q = p;
        q.y *= -1;
        q.y += 0.45;
        float d1 = fCone(q, 0.4, 5.0);
        d1 = max(d1, -fCone(q+vec3(0,0.1,0), 0.37, 5.0));
        d1 = max(d1, q.y-0.9);
        d = d1;
        p.y += 1.0;
    }

    // {
    //     d = fCylinder(p, 0.5, 0.2);
    //     p.y += 0.25;
    // }

    // {
    //     vec3 q = p/0.5;
    //     float h = 0.62;
    //     float r = 0.1;
    //     float a = 3.0;
    //     float d1 = fCylinder(q, r, h);
    //     d1 = min(d1, fCylinder(q - vec3(a,0,0), r, h));
    //     d1 = min(d1, fBox(q - vec3(0,-h,0), vec3(1.0,0.01,1.0)));
    //     d1 = min(d1, fBox(q - vec3(a,-h,0), vec3(1.0,0.01,1.0)));
    //     d1 = min(d1, fBox(q - vec3(a*0.5,+h+0.5,0), vec3(a*0.6,0.5,r*1.25)));
    //     d = d1*0.5;
    //     p.y += 0.33;
    // }
    #endif

    // smooth union
    #if 0
    float d1 = fBox(p + vec3(0.5,0.01,0.5), vec3(0.5,0.5,0.5));
    // float d2 = fBox(p + vec3(0,0.5,0.5), vec3(0.5,0.5,0.1));
    float d2 = fSphere(p - vec3(0,0,0), 0.5);
    // float d = min(d1,d2);
    float d = fOpUnionSoft(d1,d2,0.5);
    // float d = fOpUnionSoft(d1,d2,0.1);
    p.y += 0.5;
    #endif

    #if 1
    // incorrect Euclidean distance
    float d = fBox(p, vec3(0.5));
    float d2 = fBox(p, vec3(1.0,0.6,0.25));
    d = max(d, -d2);

    // correct Euclidean distance
    // float d = fBox(p-vec3(0,0,0.375), vec3(0.5,0.5,0.125));
    // d = min(d, fBox(p+vec3(0,0,0.375), vec3(0.5,0.5,0.125)));
    p.y += 0.2;
    #endif

    // discontinuity in repetition
    #if 0
    vec3 q = p;
    p.z = mod(p.z + 0.8, 1.6) - 0.8;
    // p.xz = Rotate(p.xz, 0.5);
    float d = fBox(p, vec3(0.1, 0.5, 0.6));
    p.y += 0.5;
    #endif

    #if 0
    return vec4(0.1,0.3,0.4,d); // blue
    // return vec4(0.6,0.1,0.1,d); // red
    #endif

    // Only draw isolines
    #if 1
    p.y -= 0.5;
    vec3 iso3;
    {
        float w = 0.1;
        float a = mod(d, w);
        float h = 0.02;
        float t = smoothstep(0.01,0.01+h,a)-smoothstep(w-h,w,a);
        t = mix(t, 1.0, smoothstep(0.6,0.7,d)); // limit influence
        if (d > 0.5*w)
            iso3 = mix(vec3(0.6,0.1,0.1), vec3(1.0), t);
        else
            iso3 = mix(vec3(0.1,0.3,0.4), vec3(1.0), t);
    }
    float ground = fBox(p - vec3(0,-1,0), vec3(2,1,2));
    return vec4(iso3,ground);
    #endif

    // DRAW |grad f| - 1
    #if 0
    p.y -= 0.5;
    vec3 iso3;
    {
        float t = gradf - 1.0;
        iso3 = mix(vec3(0.6,0.1,0.1), vec3(1.0), exp(-10.0*t*t));
    }
    float ground = fBox(p - vec3(0,-1,0), vec3(2,1,2));
    if (ground < d) return vec4(iso3,ground);
    else return vec4(0.123,0.49,0.144,d); // green
    #endif

    // DRAW ISOLINES
    #if 0
    p.y -= 0.5;
    vec3 iso3;
    {
        float w = 0.1;
        float a = mod(d, w);
        float h = 0.02;
        float t = smoothstep(0.01,0.01+h,a)-smoothstep(w-h,w,a);
        t = mix(t, 1.0, smoothstep(0.6,0.7,d)); // limit influence
        iso3 = mix(vec3(0.6,0.1,0.1), vec3(1.0), t);
    }
    float ground = fBox(p - vec3(0,-1,0), vec3(2,1,2));
    if (ground < d) return vec4(iso3,ground);
    else return vec4(0.123,0.49,0.144,d); // green
    #endif

    #if 0
    // p.y += 0.12;
    // p.y += 0.5;
    // float ground = fSphere(p - vec3(0,-3,0), 3);
    float ground = fBox(p - vec3(0,-1,0), vec3(2,1,2));
    if (ground < d) return vec4(1,1,1,ground);
    else return vec4(0.6,0.1,0.1,d);
    // else return vec4(0.123,0.49,0.144,d); // green
    // else return vec4(0.1,0.3,0.4,d);
    // else return vec4(0.2,0.5,0.6,d);
    #endif
}

vec3 Normal(vec3 p)
{
    vec2 e = vec2(NORMAL_EPSILON, 0.0);
    return normalize(vec3(
                     Scene(p + e.xyy).w - Scene(p - e.xyy).w,
                     Scene(p + e.yxy).w - Scene(p - e.yxy).w,
                     Scene(p + e.yyx).w - Scene(p - e.yyx).w)
    );
}

bool
Trace(vec3 origin, vec3 dir, out vec3 hit_point, out vec3 hit_albedo)
{
    const float step_scale = 1.0; // set to < 1 if deforming
    float t = 0.0;
    hit_point = origin;
    for (int i = 0; i < STEPS; i++)
    {
        vec4 s = Scene(hit_point);
        t += step_scale * s.w;
        hit_point += step_scale * s.w * dir;
        if (s.w <= EPSILON)
        {
            hit_albedo = s.rgb;
            return true;
        }
        if (t > Z_FAR)
            break;
    }
    return false;
}

vec2 seed = vec2(-1,1)*(time + 1.0);
// vec2 seed = texel * vec2(-1, 1) * (time + 1.0);
vec2 Noise2f() {
    seed += vec2(-1, 1);
    // implementation based on: lumina.sourceforge.net/Tutorials/Noise.html
    return vec2(fract(sin(dot(seed.xy, vec2(12.9898, 78.233))) * 43758.5453),
        fract(cos(dot(seed.xy, vec2(4.898, 7.23))) * 23421.631));
}

// See http://lolengine.net/blog/2013/09/21/picking-orthogonal-vector-combing-coconuts
vec3 Ortho(vec3 v)
{
    return abs(v.x) > abs(v.z) ? vec3(-v.y, v.x, 0.0)
                               : vec3(0.0, -v.z, v.y);
}

vec3 UniformHemisphereSample(vec3 normal)
{
    /*
    The general monte carlo sampler is
        F = (1 / N) sum (f(xi) / pdf(xi))

    We wish to estimate the integral

        Lo = int [(c / pi) Li cos(t) dw]

    Let's use uniform sampling (pdf = 1/2pi)
        Lo = (1 / N) sum [ ((c / pi) Li cos (t)) / (1/2pi) ]
           = (2c / N) sum [ Li cos(t) ]
    */

    vec3 tangent = normalize(Ortho(normal));
    vec3 bitangent = normalize(cross(normal, tangent));
    vec2 s = Noise2f();
    s.x = -1.0 + 2.0 * s.x;
    float t = s.y * PI;
    float r = sqrt(1.0 - s.x * s.x);
    return r * cos(t) * tangent +
           r * sin(t) * normal +
           s.x * bitangent;
}

vec3 CosineWeightedSample(vec3 normal)
{
    vec3 tangent = normalize(Ortho(normal));
    vec3 bitangent = normalize(cross(normal, tangent));
    vec2 s = Noise2f();

    /*
    For more efficiency we can generate proportionally
    fewer rays where the cos term is small. That is,

        pdf(x) = cos (t) / constant

    where constant = pi, by normalization over hemisphere.

        Lo = (1 / N) sum [ ((c / pi) Li cos (t)) / (cos(t)/pi) ]
           = (c / N) sum [Li]

    Yay!
    */

    // Uniform disk sample (should it be uniform?)
    float t = s.x * TWO_PI;
    float r = sqrt(s.y);

    // Project up to hemisphere
    float y = sqrt(1.0 - r * r);
    return cos(t) * r * tangent +
           sin(t) * r * bitangent +
           y * normal;
}

vec3 ConeSample(vec3 dir, float extent)
{
    vec3 tangent = normalize(Ortho(dir));
    vec3 bitangent = normalize(cross(dir, tangent));
    vec2 r = Noise2f();
    r.x *= TWO_PI;
    r.y *= 1.0 - r.y * extent;
    float oneminus = sqrt(1.0 - r.y * r.y);
    return cos(r.x) * oneminus * tangent +
           sin(r.x) * oneminus * bitangent +
           r.y * dir;
}

vec3 ComputeLight(vec3 hit, vec3 hit_albedo)
{
    #if 0
    vec3 normal = Normal(hit);
    vec3 eye = normalize(hit);
    vec3 origin = hit + normal * 2.0 * EPSILON;

    vec3 result = vec3(0.0);
    for (int i = 0; i < 16; i++)
    {
        vec3 dir = CosineWeightedSample(normal);
        vec3 albedo = vec3(0.0);
        if (!Trace(origin, dir, hit, albedo))
        {
            result += SampleSky(dir)/16.0;
        }
    }

    {
        vec3 sundir = normalize(vec3(-0.2, 0.4, 1.0));
        vec3 albedo = vec3(0.0);
        vec3 dir = sundir;
        if (!Trace(origin, dir, hit, albedo))
        {
            result += 2.0 * SampleSky(dir) * dot(normal, dir) / PI;
        }
    }

    result *= hit_albedo;

    // {
    //     vec3 sundir = normalize(vec3(-0.2, 0.4, 1.0));
    //     vec3 reflected = reflect(eye, normal);
    //     result += SampleSky(reflected) * pow(dot(sundir, reflected),100.0) / PI;
    // }

    return result;
    #else
    vec3 normal = Normal(hit);
    vec3 origin = hit + normal * 2.0 * EPSILON;
    vec3 dir = CosineWeightedSample(normal);
    vec3 eye = normalize(hit);

    vec3 result = vec3(0.0);
    vec3 albedo = vec3(0.0);
    if (!Trace(origin, dir, hit, albedo))
    {
        result += SampleSky(dir);
    }

    // Direct lighting
    vec3 sundir = normalize(vec3(-0.2, 2.0, 1.0)); // used for houses
    // vec3 sundir = normalize(vec3(-0.2, 0.4, 1.0));
    dir = sundir;
    if (!Trace(origin, dir, hit, albedo))
    {
        result += 2.0 * SampleSky(dir) * dot(normal, dir) / PI;
    }

    result *= hit_albedo;

    // Specular
    // {
    //     vec3 reflected = reflect(eye, normal);
    //     result += SampleSky(reflected) * pow(dot(sundir, reflected),100.0) / PI;
    // }

    return result;
    #endif
}

vec2 SampleDisk()
{
    vec2 r = Noise2f();
    r.x *= TWO_PI;
    r.y = sqrt(r.y);
    float x = r.y * cos(r.x);
    float y = r.y * sin(r.y);
    return vec2(x, y);
}

vec4 ComputeNormalFromTexel(vec2 texel)
{
    vec3 film = vec3(texel.x * aspect, texel.y, -1.0 / tan_fov_h);
    vec3 dir = normalize(film);
    vec3 origin = vec3(0.0);

    origin = (view * vec4(origin, 1.0)).xyz;
    dir = normalize((view * vec4(dir, 0.0)).xyz);

    vec3 hit_point;
    vec3 hit_albedo;
    if (Trace(origin, dir, hit_point, hit_albedo))
        return vec4(Normal(hit_point),1.0);
    return vec4(0.0);
}

void main()
{
    // Perturb texel to get anti-aliasing (for free! yay!)
    vec2 sample = texel;
    sample += (-1.0 + 2.0 * Noise2f()) * 1.0 * pixel_size;
    sample *= -1.0; // Flip before passing through lens

    vec3 film = vec3(sample.x * aspect, sample.y, 0.0);
    vec3 lens_centre = vec3(0.0, 0.0, -1.0 / tan_fov_h);
    vec3 dir = normalize(lens_centre - film);
    float t = -focal_distance / dir.z;
    vec3 focus = lens_centre + dir * t;

    vec3 lens = lens_centre + lens_radius * vec3(SampleDisk(), 0.0);
    dir = normalize(focus - lens);
    vec3 origin = lens;

    origin = (view * vec4(origin, 1.0)).xyz;
    dir = normalize((view * vec4(dir, 0.0)).xyz);

    vec3 hit_point;
    vec3 hit_albedo;
    if (Trace(origin, dir, hit_point, hit_albedo))
    {
        out_color.rgb = ComputeLight(hit_point, hit_albedo);
    }
    else
    {
        out_color.rgb = SampleSky(dir);
    }

    // Visualize region in focus by a redline
    // focus = (view * vec4(focus, 1.0)).xyz;
    // vec3 focus_line = vec3(focus.x, -0.5, focus.z); // Project onto floor
    // if (length(hit_point - focus_line) <= 0.3)
    //     out_color.r = 1.0;

    out_color.a = 1.0;
}

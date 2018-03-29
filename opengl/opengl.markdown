# A life of OpenGL graphics programming

### the naive beginner

```
while (rendering) {
    glUniform1f(glGetUniformLocation(program, "pi"), 3.1415f);
}
```

### the up-front

```
GLint uniform_pi = glGetUniformLocation(program, "pi");
while (rendering) {
    glUniform1f(uniform_pi, 3.1415f);
}
```

### the scholar
<!-- ### the architect -->

```
#include <algorithm.h>
class Program {
public:
    void uniform1f(std::string name, float x) {
        if (!locs[name])
            locs[name] = glGetUniformLocation(name);
        glUniform1f(locs[name], x);
    }
private:
    GLuint program;
    std::map<std::string, GLint> locs;
};

while (rendering) {
    program.uniform1f("pi", 3.1415f);
}

```

### the engineer

```
while (rendering) {
    // todo: uniforms break if prog is reloaded!!!
    static GLint loc = glGetUniformLocation(program, "pi");
    glUniform1f(loc, 3.1415f);
}
```

### the clever

```
#define Uniform(type,prog,name,...) {                       \
        static GLint loc = glGetUniformLocation(prog,Name); \
        glUniform##type(loc,__VA_ARGS__);                   \
    }

while (rendering) {
    Uniform(1f,program,"pi", 3.1415f);
    Uniform(1i,program,"bar", 1);
    Uniform(Matrix4fv,program,"pvm", 1,GL_FALSE,pvm);
}
```

### the end
```
while (rendering) {
    glUniform1f(glGetUniformLocation(program, "pi"), 3.1415f);
}
```

# TODO
# Convert img alt text to figure text
# Change directory structure to priority-based
#   1. distance-fields/2013-07-12-distance-fields.md
#   2. procedural/2014-09-13-procedural.md
# etc

import markdown
import os
import re

f = open("../_include/header.html")
header = f.read()
f.close()

f = open("../_include/footer.html")
footer = f.read()
f.close()

def fix_img_tags(html):
    def replace_img(m):
        return "%s" % m.group(2)
    pattern = "(<p>)(<img.*/>)(</p>)"
    return re.sub(pattern, replace_img, html)

def make_post(filename, year, month, day):
    path = "../posts/%s/%s.md" % (filename, filename)
    title = ' '.join([s.capitalize() for s in filename.split('-')])
    identifier = "%s-%s-%s-%s" % (year, month, day, filename)

    if not os.path.exists(path):
        print("%s has no markdown file - skipping!" % filename)
        return ""
        
    with open(path) as f:
        html = markdown.markdown(f.read())

    html = fix_img_tags(html)

    top = """
        <p>*** 9BITSCIENCE ***</p>
        %s
    </div>
    <div id="project">
    """ % (title.upper())

    bottom = """
    </div>
    <div id="disqus_thread"></div>
    <script type="text/javascript">
        var disqus_shortname = '9bitscience';
        var disqus_url = 'http://lightbits.github.io/%s/%s/%s/%s';
        var disqus_identifier = '%s';
        var disqus_title = '%s';

        (function() {
            var dsq = document.createElement('script');
            dsq.type = 'text/javascript'; dsq.async = true;
            dsq.src = 'http://' + disqus_shortname + '.disqus.com/embed.js';
            (document.getElementsByTagName('head')[0] || document.getElementsByTagName('body')[0]).appendChild(dsq);
        })();
    </script>
    <noscript>Please enable JavaScript to view the <a href="http://disqus.com/?ref_noscript">comments powered by Disqus.</a></noscript>
    """ % (year, month, day, filename, identifier, title)

    path = "../%s/%s/%s/%s/index.html" % (year, month, day, filename)
    if not os.path.exists(os.path.dirname(path)):
        os.makedirs(os.path.dirname(path))
    with open(path, "w+") as f:
        f.write(header + top + html + bottom + footer)

    return """
    <a href="./%s/%s/%s/%s/">
        <div class="project_tile">
            <img src="./posts/%s/preview.png"></img>
            <h1>%s</h1>
        </div>
    </a>""" % (year, month, day, filename, filename, title)

def add_archive(filename, title, year, month, day):
    return """<p>%s-%s-%s <a href="archive/%s/">%s</a></p>""" % (year, month, day, filename, title)

home = """
        <p><a href="http://lightbits.github.io/">*** 9BITSCIENCE PERSONELL LOG - EMPLOYEE nr. 0000 ***</a></p>
        Searching... </br>
        Records are available, open? [y/n]</br>
        >>_</p></br>
    </div>
    <div id="home">"""

home += make_post("distance-fields",    "2014", "09", "12")
home += make_post("procedural",         "2014", "09", "12")
home += make_post("photons",            "2014", "09", "12")
home += make_post("tree",               "2014", "09", "12")
home += make_post("android",            "2014", "09", "12")
home += make_post("waves",              "2014", "09", "12")
home += make_post("snake",              "2014", "09", "12")
home += """
    </div>
    <div id="archives">
    <h1>Archives</h1>
"""
home += add_archive("compute-shaders", "Introduction to Compute Shaders", "2014", "09", "13")
home += add_archive("imgui", "Immediate Mode GUIs", "2014", "09", "10")
home += add_archive("volumetric-flow", "Volumetric Flow Particles", "2014", "09", "08")
home += add_archive("ssbo-alignment", "Shader Storage Buffer Object Alignment", "2014", "09", "01")
home += "</div>\n"
f = open("../index.html", "w+")
f.write(header + home + footer)
f.close()
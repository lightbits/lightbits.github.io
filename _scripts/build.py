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

def make_post(name, title_nice, title_raw, year, month, day):
    path = "../projects/%s/%s.md" % (name, name)
    if not os.path.exists(path):
        print("%s does not exist" % name)
        return
        
    with open(path) as f:
        html = markdown.markdown(f.read())

    html = fix_img_tags(html)

    top = """
        %s
    </div>
    <div id="project">
    """ % (title_nice.upper())

    bottom = """
    </div>
    """

    path = "../%s/%s/%s/%s/index.html" % (year, month, day, title_raw)
    if not os.path.exists(os.path.dirname(path)):
        os.makedirs(os.path.dirname(path))
    with open(path, "w+") as f:
        f.write(header + top + html + bottom + footer)


ignored_folders = ["Thumbs.db"]
projects = os.listdir("../projects")

home = """
        10 BEGIN : Select your destination
    </div>
    <div id="home">"""

projects.reverse()
for p in projects:
    if p in ignored_folders:
        continue

    comp = p.split('-')
    title_nice = ' '.join([s.capitalize() for s in comp[3:]])
    title_raw = '-'.join(comp[3:])
    year = comp[0]
    month = comp[1]
    day = comp[2]
    make_post(p, title_nice, title_raw, year, month, day)

    home += """
        <a href="./%s/%s/%s/%s/">
            <div class="project_tile">
                <img src="./projects/%s/preview.png"></img>
                <h1>%s</h1>
            </div>
        </a>""" % (year, month, day, title_raw, p, title_nice)

home += "</div>\n"
f = open("../index.html", "w+")
f.write(header + home + footer)
f.close()
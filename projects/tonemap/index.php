<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
	<meta http-equiv="Content-Type" content="text/html; charset=UTF-8" />
	<title>Retrospective 59 minutes</title>
	<link rel="stylesheet" href="http://folk.ntnu.no/simeh/style.css">
	<!-- <link rel="shortcut icon" href="http://folk.ntnu.no/simeh/favicon.png" type="image/png" /> -->
</head>

<body>

	<div id="main">

		<div id="title">
			<h1>Retrospective 59 Minutes</h1>
			<h2>Adventures in computer programming</h2>
		</div>

		<div id="pages">
			<a href="http://folk.ntnu.no/simeh/">top</a>
			 / 
			<a href="http://folk.ntnu.no/simeh/about">about</a>
			 / 
			<a href="http://folk.ntnu.no/simeh/projects" class="active_page">[projects]</a>
			 / 
			<a href="http://folk.ntnu.no/simeh/stuff">stuff</a>
			 / 
			<a href="http://folk.ntnu.no/simeh/blog">blog</a>
		</div>

		<div id="content">
		<div class="project">

			<h1>Tonemap</h1>

			<p>Tonemapping is a method used to compress really bright images into images that are displayable on the box in front you.</p>

			<p>The WebGL demo below shows a set of color ramps with brightness ranging from low, to stupidly bright. We apply a tonemap operator (the Reinhard one <a href="#reinhard">[1]</a>) to compress the data.</p>

			<?
			include 'tonemap_demo.html';
			?>

			<h2>References</h2>
			<ul>
				<li>[1] <a id="reinhard" href="Photographic Tone Reproduction for Digital Images">http://www.cs.utah.edu/~reinhard/cdrom/</a></li>
				<li>[2] <a id="imdoingitwrong" href="http://imdoingitwrong.wordpress.com/2010/08/19/why-reinhard-desaturates-my-blacks-3/">imdoingitwrong: <i>Why Reinhard desaturates my blacks</i></a></li>
			</ul>

			<h2>Code</h2>
			<p>The code for the project can be found on Github: <a href="https://github.com/lightbits/tonemap">https://github.com/lightbits/tonemap</a></p>
		</div>
		</div>
	</div>
</body>
</html>
ProcAmp
=======

![screenshot](http://i.imgur.com/XKOCrvt.png)

![gif](http://i.imgur.com/Oy0Zrff.gif)

**ProcAmp** is a utility shader for adjusting videos. It's developed to provide
optional functionalities of video playback with the [VideoPlayer] component
that is newly introduced in Unity 5.6.

System Requirements
-------------------

- Unity 5.6

ProcAmp is compatible with all the platforms that supports VideoPlayer. If you
find any problem with compatibility, plaese report it to [Issues].

Features
--------

- Basic color adjustment (brightness, contrast and saturation).
- Color balance (temperature and cyan-purple tint).
- Chroma keying and spill removal.
- Transform (trimming, scaling and position offset).
- Color overlay (fade to color)

Implementation
--------------

There are two variants of implementation of ProcAmp.

- **The ProcAmp shader** can be used with a material asset. It provides all the
  basic features of ProcAmp.

- **The ProcAmp component** can be used with a game object. It also provides
  optional features that are useful in complex setups.

Examples of Use Cases
---------------------

### Apply ProcAmp and blit to the screen.

The ProcAmp component provides a simple rendering functionality that blits the
resulting video to the screen with fit-to-screen scaling. This would be a
handy option when trying to show the video in full-screen mode.

#### Typical steps to use

- Create a VideoPlayer (drag & drop a video asset to the hierarchy).
- Change Render Mode of the VideoPlayer to "API Only".
- Add the ProcAmp component to the game object.
- That's it!

### Apply ProcAmp and show in a RawImage UI element.

When considering to support multiple resolution/aspect ratio, it's recommended
using the [RawImage] component of the UI system to handle the situation
properly. The TargetImage property of ProcAmp is used in such cases. With
setting a RawImage to the property, it starts to update the RawImage with
resulting video.

#### Typical steps to use

- Create a UI canvas and add a RawImage to it.
- Create a VideoPlayer (drag & drop a video asset to the hierarchy).
- Change Render Mode of the VideoPlayer to "API Only".
- Add the ProcAmp component to the game object.
- Set the RawImage in the canvas to Target Image of the ProcAmp.

### Use ProcAmp as a shader.

The ProcAmp shader ("Klak/Video/ProcAmp") is an unlit shader combined with the
functionalities of ProcAmp. It's useful when using a material/renderer pair to
display a video.

#### Typical steps to use

- Create a quad object ("Create" -> "3D Object" -> "Quad").
- Create a material and change shader to "Klak/Video/ProcAmp".
- Set this material to the quad object.
- Create a VideoPlayer (drag & drop a video asset to the hierarchy).
- Change Render Mode of the VideoPlayer to "Material Override".
- Set the quad object to the Renderer property.
- Change Material Property to "\_MainTex".

### Apply ProcAmp and output to a RenderTexture.

When a RenderTexture is given to the TargetTexture property, it updates the
given RenderTexture with resulting videos. This is useful when trying to use
the video with other shaders or renderers.

### Apply ProcAmp as an image effect.

The ProcAmp component works as an image effect when it's attached to a camera
object. It overlays the resulting video onto the screen with fit-to-screen
scaling.

Acknowledgement
---------------

The footage used in the example is originally created by LaBeouf, Rönkkö and
Turner. That work was released under a Creative Commons Attribution Non-
Commercial Share-Alike licence. See Acknowledgement.txt for further details.

- "\#INTRODUCTIONS" by LaBeouf, Rönkkö & Turner
  https://vimeo.com/125095515

[VideoPlayer]: https://docs.unity3d.com/560/Documentation/Manual/VideoPlayer.html
[Issues]: https://github.com/keijiro/ProcAmp/issues
[RawImage]: https://docs.unity3d.com/Manual/script-RawImage.html
[MIT]: LICENSE.md

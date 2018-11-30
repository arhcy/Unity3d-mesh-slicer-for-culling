# Unity3d-mesh-slicer-for-culling
Slices big meshes into smaller parts to get better performance with occlusion culling.
 
 <p align="center">
 <img align="center" width="60%" src="/images/scr2.png">
 </p>

I wrote this component for my project and I think it can be useful for your projects too :) Currently, it supports only slicing by X and Y axes. It's don't separates triangles if they are in different parts, so use it only for high poly meshes.  Maybe I'll improve in the future.

Using is simple.

 <p align="center">
 <img align="center" width="40%" src="/images/scr1.png">
 </p>

1. Drag here gameobject which contains MeshFilter object of mesh which you want to slice.

2. Set a default material for new parts

3. Set number of parts by vertical and horizontal

4. Press this button


You can also use second methotd, but it's rather a crutch. I just wrote it for the test and decided to left him.

To test bounds of any mesh - drag it gameobject to BoundsVisualizer and press visualize bounds.
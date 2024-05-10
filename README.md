# Projection Demo Unity Project

See companion GitHub for server code: https://github.com/alan-smith-vt/projectionDemoServer

This project is made for demonstration purposes of our novel approach to 
projecting image data onto the physical environment with sub-milimeter level
precision. This was required for our research into "Human in the Loop" automation
for bridge inspection tasks, where we needed to overlay Machine Learning (ML) model 
results of crack splines onto the physical surface for a user to interact with.

This GitHub is a pruned down version of that research, where instead of a crack
detection ML model, we instead use a simple blob detection algorithm. This 
architecture combines a server (see companion link above) operating on a computer
 and a Unity project deployed to a Hololens 2 device. The Hololens 2 will take a 
 photo of a set of black filled circles on a white background, and send this image
 to the server. The server will run the blob detection routine on this image and 
 return a set of centroid coordinates in the image space (x,y). These coordinates
 are sent back to the Hololens 2 which utilizes our projection code to apply a 
 coordinate transformation and project a red sphere onto the centroid of each 
 detected black circle in the physical environment.
 
 Credit is given to Al-Sabbag et. al. (2022) for the basic server-client architecture 
 for sending images between a Hololens and computer by utilizing HTTPS protocols within 
 Python and Unity. Credit is given to Fogerty (2016) for the HolographicImageBlend Shader code
 which allows us to transform image coordinates into floating canvas coordinates, 
 while properly accounting for all of the Hololens specific distortions and offsets.
 
 See the following dissertation (link TBD) and associated paper (link TBD) for more
 information on our approach.
 
 # Settup Steps:
 
 Install the server companion GitHub (see above) and follow the instructions therein for setup.
 
 Clone this GitHub.
 
 Install Unity editor version 2020.3.42f1. Other versions should also work but this is the version we used.
 
 Navigate to the "scene" folder and double click "main" to open the scene.
 
 Within the "MixedRealityPlayspace" game object, select the "Network Manager" game object.
 
 Update the "Ip_eduroam Desktop" field with your computer's IP address.
 
 Ensure a webcam is plugged in (or part of) the computer. The webcam photo is not used in the example, 
 but the code is looking for the Hololens camera and will break if it cannot access a camera.
 
 Ensure the Python server (companion GitHub) has been started. 
 
 Click the play button in the editor.
 
 Check the command prompt for a "GET" request from the editor. If you do not see a "GET" request,
 then the server is not properly connected to the client (editor). Double check the IP addresses.
 
 Hit "S" on the keyboard to back up in the editor to reveal the menu. Alternatively, hold "shift" to
 bring up a virtual hand, hold "ctrl" while continuing to hold "shift" and move the mouse to the right
 to rotate the hand to face the camera. The menu will "snap" to the inner side of the hand whenever the
 camera can see the flat palm gesture.
 
 Click the "Take Photo" button by either selecting it with the center cursor or by poking it with a virtual hand.
 
 Point the red rectangle photo frame at one of the green debugging surfaces.
 
 If 5 red dots appear, then the project is correctly set up.
 
 # Optional information:
 To project additional debugging information, comment out line 101 in the ProjectionManager.cs file.
 
 To save multiple server ip addresses, uncomment lines 13-15, 30-32, and 41-43 in the NetworkManager.cs file.
 
 Lines 117-123 send additional statistics to the server to be saved as a text file. These can be added onto if needed.
 
 Intrinsics and Extrensics matrices correspond to the Unity variables ProjectionMatrix and CameraToWorldMatrix respectively.
 These can be obtained from the main camera (Camera.main) or from a PhotoCaptureFrame object. The main camera is used when
 working in the editor. When deployed to the Hololens 2 device, the main camera position is at the center of the device, not
 the camera, its projection matrix does not account for all AR specific effects, and there is not a guarantee that the extrensics
 returned by main camera are at the exact timestamp of the photo. PhotoCaptureFrame solves all of these issues by properly offseting
 the camera position, accounting for additional AR specific distortions, and saves the extrensics matrix at the exact time of photo 
 capture.
 
 Because of these differences, we save both sets of these matrics to a text file on the server within the "matrix stats" folder. 
 Additionally, the extrensics matrics are visualized in 3D space when the additional projection debugging information are enabled 
 (by commenting out line 101 in ProjectionManager.cs)
 
 # Instructional Videos:

 Example Usage in Editor:
 
 [![Example Usage in Editor](https://i.ytimg.com/vi/fHQKfhuzAUc/maxresdefault.jpg)](https://www.youtube.com/watch?v=fHQKfhuzAUc&ab_channel=AlanSmith)

Example Usage when Deployed onto Hololens 2:

 [![Example Usage when Deployed onto Hololens 2](https://i.ytimg.com/vi/NEtJXZsc2X8/maxresdefault.jpg)](https://www.youtube.com/watch?v=v=NEtJXZsc2X8&ab_channel=AlanSmith)
 
# References
 
Brandon Fogerty. Holographic photo blending with photocapture.
Unity Forums, 2016. URL https://forum.unity.com/threads/holographic-photo-blending-with-photocapture.416023/.

Zaid Abbas Al-Sabbag, Chul Min Yeum, and Sriram Narasimhan. Interactive defect
quantification through extended reality. Advanced Engineering Informatics, 51:101473,
2022.

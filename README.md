# Function-Rendering
Procedurally generate a 3d model using Parametric or Implicit analytical functions input by the user.
- Parametric uses the algorithm (dont know the name) of manually creating triangle meshes as quads that joins with surrouding quads - like how you would prodecurally create a cube
- Implicit functions uses the Marching Cube Algorithm
- Works on Windows/Mac/Android
--------------
Note: A Lot of UI are just placeholders and does not work
--------------

## How to Run in Unity
1. Locate and click the Main.scene in Asset/FunctionRendering folder using the project explorer
2. Click on Play Button

#Features
- Uses a 'Programming Like' inputs function definition
- Supports sin,cos,tan as function inputs
- Save/Load as text as .func files
- Change Apperence of Generated Model - Diffuse Color,Specular,Transparency,Shininess
- Able to include a time variable 't' to animate model according to 'Timecycle' and 'Timerange'
- Undo/Redo for textfield
- Enable Wireframe Rendering
- Internal File Browser To Load .func files

## Usage
###Parametric Input
Define the parametric equation for cartesian coordinates X,Y,Z Axis Using U,V,W as incrementing variables.
Define the U,V,W Varaible "Range" And "Precision" by setting the "Domain" And "Resolution" Respectively
Example:
Cube
Domain: 0 1 0 1 0 1
Resolution 1 1 1
Code: "x = u; y = v; z = w;"

###Implicit Input
Define the implicit equation to be = 0 as "return [Equation]" and the BoundingBox(BB) settings - size,position,resolution
Object generated uses the equation to check what models to create within the bounds of the BoundingBox
Example:
Sphere
BBSize: 10 10 10
BBPosition: 0 0 0
BBResolution: 50 50 50
Code: "return 1-x^2-y^2-z^2;"
Note: Not recommanded to set BBresoulution Above "200 200 200"

###keywords
u - incremental parameter u - increment third
v - incremental parameter v - increment second
w - incremental parameter w - increment first
t - incremental parameter for time
cos,tan,sin - math trig function
                               
## How it works?
- Uses Regex to parse user inputs for Parametric and Implicit
- Uses Runtime Compilie to inject user inputs into code to generate the Model
- Parametric uses the algorithm (dont know the name) of generating a cube - there are many tutorials out there
- Implicit functions uses the Marching Cube Algorithm

# Human Detector Desktop App from Scratch

Histogram of Oriented Gradients-based Human Detector Desktop Application (using OpenCV trained SVM model)

This application shows how the HOG-based Human Detector works step by step:

1. Build Image Pyramid
2. Slide detection windows at every position in every scaled image
3. Compute HOG feature for every detection windows
4. Multiply the HOG feature of detection windows with SVM model to compute confidence score
5. Perform Non-Maximum Suppression to pick the highest scoring box in a local location (this functionality is pending)

# Optimization

I use the Square Root Approximation (SRA) technique in this [paper](https://ieeexplore.ieee.org/abstract/document/6648678) to calculate the HOG feature. I also use the proposed Gradient Vote method in that paper.

I utilize multiple threads when processing multi-scale images in Image Pyramid to handle them simultaneously.

# Output Examples (INRIA dataset)

<img src="https://github.com/dao-duc-tung/Human-Detector-App/raw/master/media/2.PNG" alt="drawing" width="300"/>

<img src="https://github.com/dao-duc-tung/Human-Detector-App/raw/master/media/1.PNG" alt="drawing" width="300"/>

<img src="https://github.com/dao-duc-tung/Human-Detector-App/raw/master/media/8.PNG" alt="drawing" width="300"/>

<img src="https://github.com/dao-duc-tung/Human-Detector-App/raw/master/media/10.PNG" alt="drawing" width="300"/>

<img src="https://github.com/dao-duc-tung/Human-Detector-App/raw/master/media/4.PNG" alt="drawing" width="300"/>

<img src="https://github.com/dao-duc-tung/Human-Detector-App/raw/master/media/7.PNG" alt="drawing" width="300"/>

<img src="https://github.com/dao-duc-tung/Human-Detector-App/raw/master/media/6.PNG" alt="drawing" width="300"/>

<img src="https://github.com/dao-duc-tung/Human-Detector-App/raw/master/media/9.PNG" alt="drawing" width="300"/>

<img src="https://github.com/dao-duc-tung/Human-Detector-App/raw/master/media/5.PNG" alt="drawing" width="300"/>

<img src="https://github.com/dao-duc-tung/Human-Detector-App/raw/master/media/3.PNG" alt="drawing" width="300"/>

# License

The purpose of this project is for understanding how to extract HOG feature step by step.

Use at your own risk.

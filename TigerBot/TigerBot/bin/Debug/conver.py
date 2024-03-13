from ultralytics import YOLO

model = YOLO("best.pt")  
#results = model.predict(source="https://ultralytics.com/images/bus.jpg")[0]

#model.export(format="onnx",imgsz=[640,640], opset=12)
success = model.export(format='onnx')
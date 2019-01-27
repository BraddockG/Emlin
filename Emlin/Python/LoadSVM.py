﻿import ModelSaveLoad as MSL
import SVMModel
import sys

allDataAsString = str(sys.argv[1])

def main():
    svm_clf = MSL.LoadModelFromJoblib("svmClf.joblib")
    print(allDataAsString)
    listOfData = allDataAsString.split(".")

    for dataString in listOfData:
        print(dataString)
        formattedData =  dataString.split(",")
        print(svm_clf.predict([formattedData]))

if __name__ == "__main__":
    sys.exit(int(main() or 0))
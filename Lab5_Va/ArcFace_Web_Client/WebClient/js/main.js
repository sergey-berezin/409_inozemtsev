const ServerLink = 'http://localhost:5240/images';
let images_to_add = [];
let selected_images = [];

$(async () => {
    try 
    {
        RefreshList();
        console.log('START!');

        const NewImage = document.getElementById('NewImage');
        NewImage.addEventListener('change', AddToList)

        const ImagePathForm = document.getElementById('ImagePathForm');
        ImagePathForm.addEventListener('submit', AddToDatabase);

        const Name = document.getElementById('DeleteAllImages');
        Name.addEventListener('click', DeleteImages);

        const Combobox1 = document.getElementById('Image1');
        Combobox1.addEventListener('change', function() {
            selected_images[0] = Combobox1.value;
            document.getElementById('Distance').innerHTML = '';
        });

        const Combobox2 = document.getElementById('Image2');
        Combobox2.addEventListener('change', function() {
            selected_images[1] = Combobox2.value;
            document.getElementById('Similarity').innerHTML = '';
        });

        const Calculate = document.getElementById('Calculate');
        Calculate.addEventListener('click', CalculateTwoImages);
    }
    catch(error) 
    { error => console.log(error); }
})

async function AddToList(event) {
    event.preventDefault();
    images_to_add = [];
    let image = event.target.files[0];
    images_to_add.push(image);
}

async function AddToDatabase(event) {
    document.getElementById('NewImage').value = '';
    event.preventDefault();
    if (images_to_add.length == 0) return;

    let name = images_to_add[0].name;
    console.log('ADDING ' + name + '...');
        
    var reader = new FileReader();
    reader.addEventListener('load', async function() {
        base64String = reader.result.replace('data:', '').replace(/^.+,/, '');

        let response = await fetch(ServerLink, {
            mode: 'cors',
            method: 'POST',
            body: JSON.stringify({
                "name": name,
                "base64String": base64String
            }),
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });
        if (response.ok) {
            let id = await response.json();
            console.log(name + ': ID = ' + id);
            RefreshList();
        }
        else
            console.log('ERROR IN ADDING ' + name + ': ' + response.status + ': ' + response.statusText);
    }, false);
    reader.readAsDataURL(images_to_add[0]);
}

async function RefreshList() {
    console.log('REFRESHING LIST...');

    images_to_add = [];
    selected_images = [];

    document.getElementById('ImagesFromDatabase').innerHTML = '';
    document.getElementById('Image1').innerHTML = '';
    document.getElementById('Image2').innerHTML = '';
    document.getElementById('Distance').innerHTML = '';
    document.getElementById('Similarity').innerHTML = '';

    let response = await fetch(ServerLink, { mode: 'cors', method: 'GET' });
    let id_array = await response.json();
    id_array.forEach(id => SearchByID(id));

    console.log('LIST REFRESHED!');
}

async function SearchByID(id) {
    console.log('ADDING NEW IMAGE...');

    let GetLink = ServerLink + "/id?id=" + id;
    let response = await fetch(GetLink, { mode: 'cors', method: 'GET' });
    if (response.ok) {
        let image = await response.json();

        const div = document.createElement('div');
        div.setAttribute('class', 'image');
        const img = document.createElement('img');
        img.setAttribute('src', 'data:image/png;base64,' + image.data);
        img.setAttribute('alt', image.name);
        img.setAttribute('width', 150);
        img.onload = () => { URL.revokeObjectURL(img.src); }
        const label = document.createElement('label');
        label.appendChild(document.createTextNode(image.name));

        div.appendChild(img);
        div.appendChild(label);

        const Container = document.getElementById('ImagesFromDatabase');
        Container.appendChild(div);

        const combobox1 = document.getElementById('Image1');
        const combobox2 = document.getElementById('Image2');
        const option1 = document.createElement('option');
        option1.setAttribute('value', image.id);
        option1.appendChild(document.createTextNode(image.name));
        const option2 = document.createElement('option');
        option2.setAttribute('value', image.id);
        option2.appendChild(document.createTextNode(image.name));
        combobox1.appendChild(option1);
        combobox2.appendChild(option2);
        combobox1.selectedIndex = -1;
        combobox2.selectedIndex = -1;
        console.log('IMAGE ' + id + ' ADDED!');
    }
    else console.log('ERROR ' + response.status + ': ' + response.statusText);
}

async function DeleteImages(event) {
    event.preventDefault();
    console.log('DELETING IMAGES...');

    let response = await fetch(ServerLink, { method: 'DELETE' });
    if (response.ok) {
        let res = await response.json();
        selected_images = [];

        document.getElementById('ImagesFromDatabase').innerHTML = '';
        document.getElementById('Distance').innerHTML = '';
        document.getElementById('Similarity').innerHTML = '';
        document.getElementById('Image1').innerHTML = '';
        document.getElementById('Image2').innerHTML = '';
        document.getElementById('Image1').selectedIndex = -1;
        document.getElementById('Image2').selectedIndex = -1;
        console.log('ALL IMAGES DELETED!');
    }
    else console.log('ERROR ' + response.status + ': ' + response.statusText);
}

async function CalculateTwoImages(event) {
    event.preventDefault();
    const combobox1 = document.getElementById('Image1');
    const combobox2 = document.getElementById('Image2');
    if (combobox1.selectedIndex == -1 || combobox2.selectedIndex == -1) return;

    console.log('STARTING CALCULATIONS...');
    let embeddings = [];
    for (let i=0; i<2; i++) {
        let GetLink = ServerLink + "/id?id=" + selected_images[i];
        let response = await fetch(GetLink, { mode: 'cors', method: 'GET' });
        if (response.ok) { 
            let image = await response.json();

            var blob = window.atob(image.embedding);
            let blob_length = blob.length / Float32Array.BYTES_PER_ELEMENT;
            let view = new DataView(new ArrayBuffer(Float32Array.BYTES_PER_ELEMENT));
            let embedding_float_array = new Float32Array(blob_length);
            let p = 0;
            for (let j=0; j<blob_length; j++) {
                p = j * 4;
                view.setUint8(0, blob.charCodeAt(p));
                view.setUint8(1, blob.charCodeAt(p+1));
                view.setUint8(2, blob.charCodeAt(p+2));
                view.setUint8(3, blob.charCodeAt(p+3));
                embedding_float_array[j] = view.getFloat32(0, true);
            }
            embeddings.push(embedding_float_array);
        }
        else console.log('ERROR ' + response.status + ': ' + response.statusText);
    }
    const length = embeddings[0].length;

    let Distance = 0;
    let Similarity= 0;
    for (let i=0; i<length; i++) {
        let sub = embeddings[0][i] - embeddings[1][i];
        Distance += sub * sub;
        Similarity += embeddings[0][i] * embeddings[1][i];
    }
    Distance = Math.sqrt(Distance);

    document.getElementById('Distance').innerHTML = 'Distance: ' + Distance.toFixed(7);
    document.getElementById('Similarity').innerHTML = 'Similarity: ' + Similarity.toFixed(7);
    console.log('CALCULATIONS FINISHED!');
}
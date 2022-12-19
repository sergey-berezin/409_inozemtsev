const ServerLink = 'http://localhost:5240/images';
let images_list = [];

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
    }
    catch(error) 
    { error => console.log(error); }
})

async function AddToList(event) {
    event.preventDefault();
    images_list = [];
    let image = event.target.files[0];
    images_list.push(image);
}

async function AddToDatabase(event) {
    document.getElementById('NewImage').value = '';
    event.preventDefault();
    if (images_list.length == 0) return;

    let name = images_list[0].name;
    console.log('ADDING ' + name + '...');
        
    var reader = new FileReader();
    reader.addEventListener('load', async function() {
        base64String = reader.result.replace('data:', '').replace(/^.+,/, '');
        console.log('string = '+ base64String);

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
    reader.readAsDataURL(images_list[0]);
}

async function RefreshList() {
    console.log('REFRESHING LIST...');

    images_list = [];
    document.getElementById('ImagesFromDatabase').innerHTML = '';
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
        document.getElementById('ImagesFromDatabase').innerHTML = '';
        console.log(res);
        console.log('ALL IMAGES DELETED!');
    }
    else console.log('ERROR ' + response.status + ': ' + response.statusText);
}